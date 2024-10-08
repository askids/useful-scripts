using System;
using System.Security.Cryptography.X509Certificates;

public class RemoveSpecificExpiredCertificates
{
    public static void Main(string[] args)
    {
        // Define the certificate subject name you want to target (example: "CN=MyCertName")
        string targetCertName = "CN=MyCertName";

        // Open the user certificate store
        using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
        {
            store.Open(OpenFlags.ReadWrite);

            // Get all certificates from the store
            var certificates = store.Certificates;

            foreach (var cert in certificates)
            {
                // Check if the certificate matches the specified name and if it is expired
                if (cert.Subject.Contains(targetCertName) && cert.NotAfter < DateTime.Now)
                {
                    Console.WriteLine($"Removing expired certificate: {cert.Subject}");

                    // Remove the expired certificate with the specific name
                    store.Remove(cert);
                }
            }

            // Close the store
            store.Close();
        }
    }
}
