using System;
using System.Windows.Forms;

namespace AmonieNomnomPublisher;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new AmoniePublisherForm());
    }
}
