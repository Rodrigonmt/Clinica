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
function agoraBrasil() {
  const agora = new Date();
  return new Date(
    agora.toLocaleString("en-US", { timeZone: "America/Sao_Paulo" })
  );
}

function criarDataConsultaBR(data, hora) {
  const dia = data.split("T")[0];
  const [ano, mes, diaMes] = dia.split("-").map(Number);
  const [h, m] = hora.split(":").map(Number);

  return new Date(ano, mes - 1, diaMes, h, m, 0, 0);
}

function diffHoras(dataFutura, dataAtual) {
  return (dataFutura.getTime() - dataAtual.getTime()) / (1000 * 60 * 60);
}

/* ===============================
   ⏰ SCHEDULER
================================ */
exports.enviarLembretesSms = onSchedule(
  {
    schedule: "every 60 minutes",
    timeZone: "America/Sao_Paulo",
    secrets: [
      TWILIO_ACCOUNT_SID,
      TWILIO_AUTH_TOKEN,
      TWILIO_FROM_NUMBER,
    ],
  },
  async () => {
    console.log("⏰ Início da execução enviarLembretesSms");

    const agora = agoraBrasil();
    console.log("🕒 Agora (BR):", agora.toString());

    /* ===============================
       🔔 VERIFICA SE SMS ESTÁ ATIVO
    ================================ */
    const notificacoesSnap = await admin
      .database()
      .ref("configuracoes/notificacoes")
      .once("value");

    if (!notificacoesSnap.exists()) {
      console.log("⚠️ Configurações de notificações não encontradas");
      return;
    }

    if (notificacoesSnap.val().NotificacoesSMS !== true) {
      console.log("🚫 Envio de SMS desativado nas configurações");
      return;
    }

    console.log("✅ Envio de SMS ATIVADO");

    // 🔐 Twilio só é instanciado se SMS estiver ativo
    const client = twilio(
      TWILIO_ACCOUNT_SID.value(),
      TWILIO_AUTH_TOKEN.value()
    );

    const consultasSnap = await admin
      .database()
      .ref("consultas")
      .once("value");

    if (!consultasSnap.exists()) {
      console.log("⚠️ Nenhuma consulta encontrada");
      return;
    }

    const consultas = consultasSnap.val();

    const configSnap = await admin
      .database()
      .ref("configuracoes/empresa")
      .once("value");

    if (!configSnap.exists()) {
      console.log("⚠️ Configurações da empresa não encontradas");
      return;
    }

    const {
      NomeEmpresa,
      Telefone: telefoneEmpresa
    } = configSnap.val();

    for (const consultaId in consultas) {
      const consulta = consultas[consultaId];

      const {
        data,
        horaInicio,
        usuario: usuarioId,
        lembreteSmsEnviado,
      } = consulta;

      if (!data || !horaInicio || !usuarioId) continue;
      if (lembreteSmsEnviado === true) continue;

      const dataHoraConsulta = criarDataConsultaBR(data, horaInicio);
      if (isNaN(dataHoraConsulta.getTime())) continue;

      const horas = diffHoras(dataHoraConsulta, agora);

      if (horas > 23 && horas <= 24) {
        const usuarioSnap = await admin
          .database()
          .ref(`usuarios/${usuarioId}`)
          .once("value");

        if (!usuarioSnap.exists()) continue;

        const telefone = usuarioSnap.val().Telefone;
        if (!telefone) continue;

        const mensagem =
          `Olá!\n\n` +
          `${NomeEmpresa}.\n\n` +
          `Lembrete agendamento.\n\n` +
          `Data: ${dataHoraConsulta.toLocaleDateString("pt-BR")}\n` +
          `Hora: ${horaInicio}\n\n` +
          `Contato: ${telefoneEmpresa}!`;

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