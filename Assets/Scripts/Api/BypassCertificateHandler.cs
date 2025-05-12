using UnityEngine.Networking;
// System.Security.Cryptography.X509Certificates no es necesario si solo retornas true.

public class BypassCertificateHandler : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        // Simplemente acepta todos los certificados.
        // ¡PELIGROSO para producción! Solo para desarrollo local con http o certificados auto-firmados.
        return true;
    }
}