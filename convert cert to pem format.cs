using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

public static class CertificateExtensions
{
    /// <summary>
    /// Exports a certificate by its label (subject name) to a PEM file, from the specified store location.
    /// Only exports the certificate if it is currently active (valid).
    /// </summary>
    /// <param name="storeLocation">The certificate store location (e.g., LocalMachine, CurrentUser).</param>
    /// <param name="subjectName">The subject name (label) of the certificate to export.</param>
    /// <param name="outputFileName">The output file path where the PEM file will be saved.</param>
    /// <param name="logger">ILogger instance for logging.</param>
    public static void ExportCertificateToPem(this StoreLocation storeLocation, string subjectName, string outputFileName, ILogger logger)
    {
        // Open the store for the specified location and StoreName.My
        using (X509Store store = new X509Store(StoreName.My, storeLocation))
        {
            store.Open(OpenFlags.ReadOnly);

            // Find the certificate by its subject name and ensure it is valid (active)
            X509Certificate2 cert = null;
            foreach (X509Certificate2 c in store.Certificates)
            {
                // Check if the subject name matches and the certificate is valid (not expired)
                if (c.Subject.Equals(subjectName, StringComparison.OrdinalIgnoreCase) && c.NotAfter > DateTime.Now && c.NotBefore <= DateTime.Now)
                {
                    cert = c;
                    break;
                }
            }

            // Check if the certificate is found and valid
            if (cert != null)
            {
                // Read raw certificate data (DER encoded)
                byte[] certBytes = cert.RawData;

                // Convert the byte array to a Base64 string
                string certPem = "-----BEGIN CERTIFICATE-----\n" +
                                 Convert.ToBase64String(certBytes, Base64FormattingOptions.InsertLineBreaks) +
                                 "\n-----END CERTIFICATE-----";

                // Check if the .pem file already exists
                if (File.Exists(outputFileName))
                {
                    // Read the existing file content
                    string existingPem = File.ReadAllText(outputFileName);
                    
                    // Compare existing PEM with the newly generated PEM
                    if (existingPem.Equals(certPem, StringComparison.Ordinal))
                    {
                        logger.LogInformation("The existing PEM file is up to date. No changes made to: {OutputFileName}", outputFileName);
                        return; // Skip overwriting if the file is the same
                    }
                }

                // Write the certificate to a .pem file, overwriting if necessary
                File.WriteAllText(outputFileName, certPem);
                logger.LogInformation("Certificate exported successfully to: {OutputFileName}", outputFileName);
            }
            else
            {
                logger.LogWarning("Valid certificate not found with the specified subject name: {SubjectName}", subjectName);
            }
        }
    }
}
