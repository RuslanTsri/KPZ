using System;
using System.Drawing.Printing;
using System.Drawing;

namespace Lab6.Services
{
    public class PrintService : IPrintService
    {
        public void PrintQRCode(string filePath)
        {
            PrintDocument printDoc = new PrintDocument();
            printDoc.PrintPage += (sender, e) => HandlePrintPage(e, filePath);
            printDoc.Print();
        }

        private void HandlePrintPage(PrintPageEventArgs e, string filePath)
        {
            try
            {
                using (Image image = Image.FromFile(filePath))
                {
                    e.Graphics.DrawImage(image, 10, 10, 300, 300);
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        private void LogError(Exception ex)
        {
            // Можна замінити на більш розширену систему логування
            Console.WriteLine($"[PrintService] Error printing QR Code: {ex.Message}");
        }
    }
}
