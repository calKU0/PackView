using Hydra;
using HydraPackView.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

[assembly: CallbackAssemblyDescription("Podgląd pakowania",
"PackView",
"Krzysztof Kurowski",
"1.0",
"8.0",
"09-07-2025")]

namespace HydraPackView
{
    public class Class1
    {
        private static readonly string filePath = @"\\backup\k\Dodatki\PackView\PackViewApp.exe";

        #region FS

        [SubscribeProcedure((Procedures)Procedures.TrN_FS, "callback na FS")]
        public class callbacktestowyFS : Callback
        {
            private readonly string _connectionString = Runtime.ActiveRuntime.Repository.Connection.ConnectionString + ";Trusted_Connection=yes;connection timeout=5";
            private DatabaseService _databaseService;

            private ClaWindow button;
            private ClaWindow buttonParent;

            public override void Init()
            {
                AddSubscription(true, 0, Events.OpenWindow, new TakeEventDelegate(OnOpenWindow));
                AddSubscription(false, 0, Events.ResizeWindow, new TakeEventDelegate(ChangeWindow));
            }

            public bool OnOpenWindow(Procedures ProcId, int ControlId, Events Event)
            {
                ClaWindow parent = GetWindow();
                _databaseService = new DatabaseService(_connectionString);
                buttonParent = parent.AllChildren["?TnO:Opis:Prompt"]; // od ktorego przycisku
                button = parent.Children["?TabNaglowek"].Children.Add(ControlTypes.button); // w ktorej belce
                button.Visible = true;

                button.TextRaw = "Podgląd pakowania";
                button.Bounds = new Rectangle(Convert.ToInt32(buttonParent.XposRaw) - 3, Convert.ToInt32(buttonParent.YposRaw) + 10, 60, 20);

                AddSubscription(false, button.Id, Events.Accepted, new TakeEventDelegate(NewButton_OnAfterMouseDown));

                return true;
            }

            public bool ChangeWindow(Procedures ProcId, int ControlId, Events Event)
            {
                button.Bounds = new Rectangle(Convert.ToInt32(buttonParent.XposRaw) - 3, Convert.ToInt32(buttonParent.YposRaw) + 10, 60, 20);
                return true;
            }

            public bool NewButton_OnAfterMouseDown(Procedures ProcedureId, int ControlId, Events Event)
            {
                try
                {
                    if (_databaseService.IsPacked(TraNag.TrN_GIDNumer))
                    {
                        string arguments = string.Empty;
                        using (SelectProducts productsForm = new SelectProducts(_databaseService, TraNag.TrN_GIDNumer))
                        {
                            var formResult = productsForm.ShowDialog();
                            if (formResult != DialogResult.OK)
                            {
                                return true;
                            }

                            List<string> results = productsForm.ReturnProducts;
                            if (results == null || !results.Any())
                            {
                                MessageBox.Show("Nie wybrano żadnego towaru do podgladu.", "Informacja", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                return true;
                            }

                            // Encode and pass as separate arguments: [0] = product list, [1] = GID number
                            string productArg = string.Join(";", results.Select(EncodeArg));
                            string gidArg = TraNag.TrN_GIDNumer.ToString();

                            arguments = $"\"{productArg}\" \"{gidArg}\"";
                        }

                        ManualResetEvent formReadyEvent = new ManualResetEvent(false);
                        LoadingForm loadingForm = null;

                        Thread loadingThread = new Thread(() =>
                        {
                            loadingForm = new LoadingForm();
                            formReadyEvent.Set();
                            Application.Run(loadingForm);
                        });
                        loadingThread.SetApartmentState(ApartmentState.STA);
                        loadingThread.Start();

                        formReadyEvent.WaitOne();

                        Task.Run(() =>
                        {
                            try
                            {
                                var startInfo = new ProcessStartInfo
                                {
                                    FileName = filePath,
                                    Arguments = arguments,
                                    UseShellExecute = false
                                };

                                using (var process = new Process { StartInfo = startInfo })
                                {
                                    process.Start();
                                    process.WaitForExit();
                                }
                            }
                            finally
                            {
                                if (loadingForm != null)
                                {
                                    while (!loadingForm.IsHandleCreated)
                                        Thread.Sleep(10);

                                    if (!loadingForm.IsDisposed)
                                    {
                                        loadingForm.Invoke(new MethodInvoker(() =>
                                        {
                                            loadingForm.Close();
                                        }));
                                    }
                                }
                            }
                        });
                    }
                    else
                    {
                        MessageBox.Show("Faktura nie została jeszcze spakowana", "Informacja", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Błąd: " + ex.Message, "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                return true;
            }

            public override void Cleanup()
            {
            }

            private string EncodeArg(string s) => s.Replace("|", "\\|").Replace(";", "\\;").Replace("\"", "\\\"");
        }

        #endregion FS

        #region FSE

        [SubscribeProcedure((Procedures)Procedures.TrN_FSE, "callback na FSE")]
        public class callbacktestowyFSE : Callback
        {
            private ClaWindow button;
            private ClaWindow buttonParent;
            private string bazaSQL = Hydra.Runtime.Config.Baza;
            private DatabaseService _databaseService;

            public override void Init()
            {
                AddSubscription(true, 0, Events.OpenWindow, new TakeEventDelegate(OnOpenWindow)); // Otwarcie okna
                AddSubscription(false, 0, Events.ResizeWindow, new TakeEventDelegate(ChangeWindow)); // zmiana szerokosci/wysokosci okna
            }

            public bool OnOpenWindow(Procedures ProcId, int ControlId, Events Event)
            {
                ClaWindow Parent = GetWindow();
                _databaseService = new DatabaseService(bazaSQL);
                buttonParent = Parent.AllChildren["?TnO:Opis"]; // od ktorego przycisku
                button = Parent.Children["?TabNaglowek"].Children.Add(ControlTypes.button); // w ktorej belce
                button.Visible = true;

                button.TextRaw = "Podgląd pakowania";

                button.Bounds = new Rectangle(Convert.ToInt32(buttonParent.XposRaw) - 63, Convert.ToInt32(buttonParent.YposRaw) + 60, 60, 15);

                AddSubscription(false, button.Id, Events.Accepted, new TakeEventDelegate(NewButton_OnAfterMouseDown));

                return true;
            }

            public bool ChangeWindow(Procedures ProcId, int ControlId, Events Event)
            {
                button.Bounds = new Rectangle(Convert.ToInt32(buttonParent.XposRaw) - 63, Convert.ToInt32(buttonParent.YposRaw) + 60, 60, 15);
                return true;
            }

            public bool NewButton_OnAfterMouseDown(Procedures ProcedureId, int ControlId, Events Event)
            {
                try
                {
                    if (_databaseService.IsPacked(TraNag.TrN_GIDNumer))
                    {
                        (string arg1, string arg2, string arg3) = _databaseService.CheckPackingDates(TraNag.TrN_GIDNumer);
                        string arguments = $"\"{arg1}\" \"{arg2}\" \"{arg3}\"";

                        ProcessStartInfo startInfo = new ProcessStartInfo();
                        startInfo.FileName = filePath;
                        startInfo.Arguments = arguments;

                        Process process = new Process();
                        process.StartInfo = startInfo;
                        Thread th = new Thread(() =>
                        {
                            process.Start();
                        });
                        th.Start();
                    }
                    else
                    {
                        MessageBox.Show("Faktura nie została jeszcze spakowana");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Błąd: " + ex);
                }

                return true;
            }

            public override void Cleanup()
            {
            }
        }

        #endregion FSE

        #region FS spinacz

        [SubscribeProcedure((Procedures)Procedures.TrN_FSSpinacz, "callback na FS")]
        public class callbacktestowyFSSpinacz : callbacktestowyFS
        { }

        #endregion FS spinacz

        #region PA

        [SubscribeProcedure((Procedures)Procedures.TrN_PA, "callback na PA")]
        public class callbacktestowyPA : callbacktestowyFS
        { }

        #endregion PA

        #region WZ

        [SubscribeProcedure((Procedures)Procedures.TrN_WZ, "callback na WZ")]
        public class callbacktestowyWZ : callbacktestowyFS
        { }

        #endregion WZ

        #region PA

        [SubscribeProcedure((Procedures)Procedures.TrN_WZE, "callback na WZE")]
        public class callbacktestowyWZE : callbacktestowyFS
        { }

        #endregion PA
    }
}