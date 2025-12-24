const admin = require("firebase-admin");
const { onSchedule } = require("firebase-functions/v2/scheduler");
const { defineSecret } = require("firebase-functions/params");
const twilio = require("twilio");

admin.initializeApp();

/* ===============================
   🔐 SECRETS TWILIO
================================ */
const TWILIO_ACCOUNT_SID = defineSecret("TWILIO_ACCOUNT_SID");
const TWILIO_AUTH_TOKEN = defineSecret("TWILIO_AUTH_TOKEN");
const TWILIO_FROM_NUMBER = defineSecret("TWILIO_FROM_NUMBER");

/* ===============================
   🕒 HELPER PADRÃO DATA BR
================================ */

/**
 * Retorna Date no horário do Brasil (America/Sao_Paulo)
 */
function agoraBrasil() {
  const agora = new Date();
  return new Date(
    agora.toLocaleString("en-US", { timeZone: "America/Sao_Paulo" })
  );
}

/**
 * Cria Date da consulta no horário do Brasil
 * @param {string} data yyyy-mm-dd ou yyyy-mm-ddTHH:mm:ss
 * @param {string} hora HH:mm
 */
function criarDataConsultaBR(data, hora) {
  const dia = data.split("T")[0]; // yyyy-mm-dd
  const [ano, mes, diaMes] = dia.split("-").map(Number);
  const [h, m] = hora.split(":").map(Number);

  // ⚠️ month é 0-based
  return new Date(ano, mes - 1, diaMes, h, m, 0, 0);
}

/**
 * Diferença em horas entre duas datas
 */
function diffHoras(dataFutura, dataAtual) {
  return (dataFutura.getTime() - dataAtual.getTime()) / (1000 * 60 * 60);
}

/* ===============================
   ⏰ SCHEDULER
================================ */
exports.enviarLembretesSms = onSchedule(
  {
    schedule: "every 5 minutes", // 🔁 depois volte para every 60 minutes
    timeZone: "America/Sao_Paulo",
    secrets: [
      TWILIO_ACCOUNT_SID,
      TWILIO_AUTH_TOKEN,
      TWILIO_FROM_NUMBER,
    ],
  },
  async () => {
    console.log("⏰ Início da execução enviarLembretesSms");

    const client = twilio(
      TWILIO_ACCOUNT_SID.value(),
      TWILIO_AUTH_TOKEN.value()
    );

    const agora = agoraBrasil();
    console.log("🕒 Agora (BR):", agora.toString());

    const consultasSnap = await admin
      .database()
      .ref("consultas")
      .once("value");

    if (!consultasSnap.exists()) {
      console.log("⚠️ Nenhuma consulta encontrada");
      return;
    }

    const consultas = consultasSnap.val();

    for (const consultaId in consultas) {
      const consulta = consultas[consultaId];

      const {
        data,
        horaInicio,
        usuario: usuarioId,
        lembreteSmsEnviado,
      } = consulta;

      if (!data || !horaInicio || !usuarioId) {
        console.log(`⚠️ Consulta ${consultaId} incompleta`);
        continue;
      }

      if (lembreteSmsEnviado === true) {
        console.log(`ℹ️ SMS já enviado para ${consultaId}`);
        continue;
      }

      // ✅ DATA DA CONSULTA (BR)
      const dataHoraConsulta = criarDataConsultaBR(data, horaInicio);

      if (isNaN(dataHoraConsulta.getTime())) {
        console.log(`❌ Data inválida na consulta ${consultaId}`, data, horaInicio);
        continue;
      }

      const horas = diffHoras(dataHoraConsulta, agora);

      console.log(
        `📌 Consulta ${consultaId}
        🗓️ Consulta: ${dataHoraConsulta.toString()}
        ⏳ Diferença horas: ${horas.toFixed(2)}`
      );

      /* ===============================
         ⏳ JANELA SEGURA DE ENVIO
         Entre 23h e 24h
      ================================ */
      if (horas > 23 && horas <= 24) {
        const usuarioSnap = await admin
          .database()
          .ref(`usuarios/${usuarioId}`)
          .once("value");

        if (!usuarioSnap.exists()) {
          console.log(`⚠️ Usuário ${usuarioId} não encontrado`);
          continue;
        }

        const telefone = usuarioSnap.val().Telefone;

        if (!telefone) {
          console.log(`⚠️ Usuário ${usuarioId} sem telefone`);
          continue;
        }

        const mensagem =
          `Olá! 😊 Este é um lembrete do seu agendamento.\n\n` +
          `📅 Data: ${dataHoraConsulta.toLocaleDateString("pt-BR")}\n` +
          `⏰ Horário: ${horaInicio}\n\n` +
          `Em caso de dúvida, estamos à disposição!`;

        try {
          console.log(`📤 Enviando SMS para ${telefone}`);

          const result = await client.messages.create({
            to: telefone,
            from: TWILIO_FROM_NUMBER.value(),
            body: mensagem,
          });

          console.log(`✅ SMS enviado | SID: ${result.sid}`);

          await admin
            .database()
            .ref(`consultas/${consultaId}`)
            .update({
              lembreteSmsEnviado: true,
              lembreteSmsEnviadoEm: agora.toISOString(),
            });
        } catch (error) {
          console.error(
            `❌ Erro ao enviar SMS para ${telefone}`,
            error.message,
            error.code
          );
        }
      }
    }

    console.log("✅ Execução finalizada");
  }
);
