const { onRequest } = require("firebase-functions/v2/https");
const { defineSecret } = require("firebase-functions/params");
const admin = require("firebase-admin");
const nodemailer = require("nodemailer");

admin.initializeApp();

// 🔐 Secrets (não criam env normal)
const GMAIL_USER = defineSecret("GMAIL_USER");
const GMAIL_PASS = defineSecret("GMAIL_PASS");

exports.enviarEmailVerificacao = onRequest(
  {
    region: "us-central1",
    secrets: [GMAIL_USER, GMAIL_PASS],
  },
  async (req, res) => {
    try {
      const { email } = req.body;

      if (!email) {
        return res.status(400).json({ erro: "Email é obrigatório" });
      }

      const transporter = nodemailer.createTransport({
        service: "gmail",
        auth: {
          user: GMAIL_USER.value(),
          pass: GMAIL_PASS.value(),
        },
      });

      const link = await admin.auth().generateEmailVerificationLink(email, {
        url: "https://example.com",
      });

      await transporter.sendMail({
        from: `"AgendaPro" <${GMAIL_USER.value()}>`,
        to: email,
        subject: "Confirme seu e-mail",
        html: `
          <p>Olá!</p>
          <p>Clique no botão abaixo para validar seu e-mail:</p>
          <p>
            <a href="${link}"
               style="padding:10px 15px;background:#4CAF50;color:#fff;text-decoration:none;">
               Confirmar e-mail
            </a>
          </p>
        `,
      });

      res.status(200).json({ sucesso: true });
    } catch (error) {
  console.error("ERRO DETALHADO:", error);
  // Isso vai te mostrar no console do Firebase ou no retorno do CMD o erro real
  res.status(500).json({ 
    erro: "Erro ao enviar e-mail", 
    detalhes: error.message 
  });
    }
  }
);
