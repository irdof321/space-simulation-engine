using System.Windows.Forms;

internal static class LiveSimulationRunner
{
    public static void Run()
    {
        LiveSimulationSession session = LiveSimulationSession.Create();
        session.PrintIntro();
        session.PrintSample();

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        using var form = new OrbitalLiveForm(session);
        Application.Run(form);
        session.PrintCompletion(form.Completed);
    }
}
