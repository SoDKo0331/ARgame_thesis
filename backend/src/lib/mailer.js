import { env } from "../config/env.js";

let transporterPromise;

async function getTransporter() {
  if (!transporterPromise) {
    transporterPromise = import("nodemailer").then(({ default: nodemailer }) =>
      nodemailer.createTransport({
        service: "gmail",
        auth: {
          user: env.gmailUser,
          pass: env.gmailAppPassword
        }
      })
    );
  }

  return transporterPromise;
}

export function canSendEmail() {
  return Boolean(env.gmailUser && env.gmailAppPassword && env.mailFrom);
}

export async function sendVerificationEmail({ email, code }) {
  if (!canSendEmail()) {
    if (!env.allowConsoleOtpFallback) {
      throw new Error("Gmail SMTP is not configured for OTP delivery");
    }

    console.info(`[OTP:FALLBACK] ${email} => ${code}`);
    return { deliveryMethod: "console" };
  }

  const transporter = await getTransporter();

  await transporter.sendMail({
    from: env.mailFrom,
    to: email,
    subject: "Your Nomad Routes verification code",
    text: `Your verification code is ${code}. It expires in ${env.otpExpiresInMinutes} minutes.`,
    html: `
      <div style="font-family: Arial, sans-serif; padding: 24px; color: #303841;">
        <h2 style="margin: 0 0 12px;">Nomad Routes verification</h2>
        <p style="margin: 0 0 16px;">Use the code below to verify your email address.</p>
        <div style="display: inline-block; padding: 12px 18px; border: 3px solid #303841; background: #EEEEEE; font-size: 28px; font-weight: 700; letter-spacing: 6px;">
          ${code}
        </div>
        <p style="margin: 16px 0 0;">This code expires in ${env.otpExpiresInMinutes} minutes.</p>
      </div>
    `
  });

  return { deliveryMethod: "email" };
}
