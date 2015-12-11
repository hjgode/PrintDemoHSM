using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using System.Windows;

using HSM.Mobility.Printing;
using Windows.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Windows.Networking.Proximity;
using Windows.UI.Popups;
using System.Threading.Tasks;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace PrintDemoHSM
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        LinePrinter linePrinter;
        public const string JSON_FILE = "printer_profiles.JSON";

        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;

            LoadPrinters();
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Prepare page for display here.

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.
        }

        int checkError(int iFunction, string s)
        {
            System.Diagnostics.Debug.WriteLine("function '"+s+"'  return code = " + iFunction.ToString());
            return iFunction;
        }
        private async void button_Click(object sender, RoutedEventArgs e)
        {

            StorageFolder InstallationFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            try {
                String sPrinter = (String)comboBox.SelectedValue;
                //var peerList = await PeerFinder.FindAllPeersAsync();
                btPeer pi = (btPeer)comboBoxBTpeers.SelectedItem;
                String sBTMAC = pi._pi.HostName.ToString();
                sBTMAC = sBTMAC.Replace(":", "");
                sBTMAC = sBTMAC.Replace("(", "");
                sBTMAC = sBTMAC.Replace(")", "");

                System.Diagnostics.Debug.WriteLine("About to print: {0}, {1}", sPrinter, sBTMAC);

                //### 1. ### 
                //load new LinePrinter
                if (linePrinter != null)
                {
                    linePrinter.Disconnect();
                    linePrinter.Close();
                    linePrinter = null;
                }
                linePrinter = new LinePrinter(JSON_FILE, sPrinter, sBTMAC);

                if (linePrinter == null)
                    throw new Exception("new Lineprinter() failed");
                int iRet;
#if DEBUG
                // create a file with the given filename in the local folder; replace any existing file with the same name
                StorageFile filePrint = await Windows.Storage.ApplicationData.Current.TemporaryFolder.CreateFileAsync("esp.txt", CreationCollisionOption.ReplaceExisting);

                string sPrintFile = "esp.txt";// filePrint.Path;

                //### 2. ### 
                //GetPrinthandle()
                iRet = checkError(linePrinter.GetPrintHandle(), "GetPrintHandle()");
                System.Diagnostics.Debug.WriteLine("GetPrintHandle() returned: " + ((PrinterError)(iRet)).ToString());

                iRet = linePrinter.StartFileEcho(sPrintFile, false);
#else
                checkError(linePrinter.GetPrintHandle(), "GetPrintHandle()");
#endif

                iRet = checkError(linePrinter.Connect(), "Connect"); 
                if (iRet != 0)
                    throw new Exception("Connect failed with error code=" + iRet.ToString());

                int iLinePrinter = checkError(linePrinter.GetPrintHandle(), "GetPrintHandle()");    //addidtional step needed BEFORE accessing the printer!

                //### 3. ###
                //register the callbacks
                iRet = checkError( linePrinter.RegisterErrorEvent(ErrorStatus), "RegisterErrorEvent");
                iRet = checkError( linePrinter.RegisterProgressEvent(progressStatus), "RegisterProgressEvent");

#if DEBUG

                iRet = checkError( linePrinter.Write("hello"), "Write hello");
                iRet = checkError( linePrinter.NewLine(1), "NewLine");
#else
                printReceipt(ref linePrinter);
#endif

                checkError( linePrinter.Flush(), "flush");
                checkError(linePrinter.EndDoc(), "EndDoc");
                checkError(linePrinter.Disconnect(), "Disconnect");
                checkError(linePrinter.Close(), "Close");
#if DEBUG
                checkError(linePrinter.StopFileEcho(), "StopFileEcho");
                StorageFile file = await Windows.Storage.ApplicationData.Current.TemporaryFolder.GetFileAsync("esp.txt");
                Stream stream = await file.OpenStreamForReadAsync();
                StreamReader sr = new StreamReader(stream);
                String sPrinted = sr.ReadToEnd();
                System.Diagnostics.Debug.WriteLine("+++++++++++++++++++++++++++++++++++++++++++++++++++");
                System.Diagnostics.Debug.WriteLine(sPrinted);
                System.Diagnostics.Debug.WriteLine("+++++++++++++++++++++++++++++++++++++++++++++++++++");
#endif
                linePrinter = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Printer Exception: " + ex.Message + "\r\n" + ex.StackTrace);
            }
        }

        /// ************************************************************************************************
        /// <summary>
        /// UpdateStatusUI
        /// </summary>
        /// <remarks>
        /// Update Status UI message
        /// <Development> Implemented. </Development>                                         
        /// ************************************************************************************************
        public bool UpdateStatusUI(Int32 progStatus)
        {
            //System.Diagnostics.Debug.WriteLine("progStatus: ", progStatus.ToString());
            return false;
        }
        /// ************************************************************************************************
        /// <summary>
        /// progressStatus
        /// </summary>
        /// <remarks>
        /// event - updates progress staut of printer
        /// <param name="handle">handle</param>
        /// <param name="progStatus">status value</param>
        /// <Development> Implemented. </Development>                                         
        /// ************************************************************************************************
        public void progressStatus(UInt64 handle, Int32 progStatus)
        {
            string s = ((PrinterProgress)progStatus).ToString();
            System.Diagnostics.Debug.WriteLine("progressStatus: handle=" + handle.ToString() +", status=" + progStatus.ToString() + ", " +s);
            //if (!UpdateStatusUI(progStatus))
            //{
            //    switch (progStatus)
            //    {
            //        case (int)PrinterProgress.MSG_CANCEL:
            //            SetStatusMsg(false, "PROGRESS_CANCEL_MSG");
            //            break;
            //        case (int)PrinterProgress.MSG_COMPLETE:
            //            SetStatusMsg(true, "PROGRESS_COMPLETE_MSG");
            //            break;
            //        case (int)PrinterProgress.MSG_ENDDOC:
            //            SetStatusMsg(true, "PROGRESS_ENDDOC_MSG");
            //            break;
            //        case (int)PrinterProgress.MSG_FINISHED:
            //            SetStatusMsg(true, "PROGRESS_FINISHED_MSG");
            //            break;
            //        case (int)PrinterProgress.MSG_STARTDOC:
            //            {
            //                //if (_bIsLinePrinter)
            //                //    EnableLinePrintingControls(true);
            //                //else
            //                //    EnableLabelPrintingControls(true);
            //                //this.btnConnect.IsEnabled = false;
            //                //this.btnDisconnect.IsEnabled = true;
            //                //if (null != _rm)
            //                    SetStatusMsg(true, "CONNECT_SUCCESS");

            //            }
            //            break;
            //        default:
            //            SetStatusMsg(false, "PROGRESS_NONE_MSG");
            //            break;
            //    }
            //}
        }

        /// ************************************************************************************************
        /// <summary>
        /// GenerateReciept 
        /// </summary>
        /// <remarks>
        /// Generate dummy Reciept 
        /// <Development> Implemented. </Development>                                         
        /// ************************************************************************************************
        private bool printReceipt(ref LinePrinter LinePrinterObject)
        {
            try
            {
                string sDocNumber = "Doc1";
                LinePrinterObject.NewLine(1);

                // Set font style to Bold + Double Wide + Double High.
                LinePrinterObject.SetBold(true);
                LinePrinterObject.SetDoubleWide(true);
                LinePrinterObject.SetDoubleHigh(true);
                LinePrinterObject.Write("SALES ORDER");
                LinePrinterObject.SetDoubleWide(false);
                LinePrinterObject.SetDoubleHigh(false);
                LinePrinterObject.NewLine(2);

                // The following text shall be printed in Bold font style.
                LinePrinterObject.Write("CUSTOMER: Casual Step");
                LinePrinterObject.SetBold(false);  // Returns to normal font.
                LinePrinterObject.NewLine(2);

                // Set font style to Compressed + Double High.
                LinePrinterObject.SetDoubleHigh(true);
                LinePrinterObject.SetCompress(true);
                LinePrinterObject.Write("DOCUMENT#: " + sDocNumber);
                LinePrinterObject.SetCompress(false);
                LinePrinterObject.SetDoubleHigh(false);
                LinePrinterObject.NewLine(2);

                // The following text shall be printed in Normal font style.
                LinePrinterObject.Write(" PRD. DESCRIPT.   PRC.  QTY.    NET.");
                LinePrinterObject.NewLine(2);

                LinePrinterObject.Write(" 1501 Timer-Md1  13.15     1   13.15");
                LinePrinterObject.NewLine(1);
                LinePrinterObject.Write(" 1502 Timer-Md2  13.15     3   39.45");
                LinePrinterObject.NewLine(1);
                LinePrinterObject.Write(" 1503 Timer-Md3  13.15     2   26.30");
                LinePrinterObject.NewLine(1);
                LinePrinterObject.Write(" 1504 Timer-Md4  13.15     4   52.60");
                LinePrinterObject.NewLine(1);
                LinePrinterObject.Write(" 1505 Timer-Md5  13.15     5   65.75");
                LinePrinterObject.NewLine(1);
                LinePrinterObject.Write("                        ----  ------");
                LinePrinterObject.NewLine(1);
                LinePrinterObject.Write("              SUBTOTAL    15  197.25");
                LinePrinterObject.NewLine(2);

                LinePrinterObject.Write("          5% State Tax          9.86");
                LinePrinterObject.NewLine(2);

                LinePrinterObject.Write("                              ------");
                LinePrinterObject.NewLine(1);
                LinePrinterObject.Write("           BALANCE DUE        207.11");
                LinePrinterObject.NewLine(1);
                LinePrinterObject.NewLine(1);

                LinePrinterObject.Write(" PAYMENT TYPE: CASH");
                LinePrinterObject.NewLine(2);

                LinePrinterObject.SetDoubleHigh(true);
                LinePrinterObject.Write("       SIGNATURE / STORE STAMP");
                LinePrinterObject.SetDoubleHigh(false);
                LinePrinterObject.NewLine(2);
                LinePrinterObject.NewLine(1);
                LinePrinterObject.SetBold(true);
                //if (_strOptional != "")
                //{
                //    // Print the text entered by user in the Optional Text field.
                //    LinePrinterObject.Write(_strOptional);
                //    LinePrinterObject.NewLine(2);
                //}
                LinePrinterObject.Write("          ORIGINAL");
                LinePrinterObject.SetBold(false);
                LinePrinterObject.NewLine(2);

                // Print a Code 39 barcode containing the document number.
                LinePrinterObject.WriteBarcode(BarcodeType.BC_CODE39,
                        sDocNumber,   // Document# to encode in barcode
                        90,           // Desired height of the barcode in printhead dots
                        40);          // Offset in printhead dots from the left of the page

                LinePrinterObject.NewLine(4);
                return true;
            }
            catch (Exception)
            {
                SetStatusMsg(false, "RECPT_ERROR_STATUS");
            }
            return false;
        }

        /// ************************************************************************************************
        /// <summary>
        /// ErrorStatus
        /// </summary>
        /// <remarks>
        ///  event - updates Error status of printing process.
        /// <param name="handle">handle</param>
        /// <param name="Errcode">error code</param>
        /// <param name="msg">error msg</param>
        /// <Development> Implemented. </Development>                                         
        /// ************************************************************************************************
        public void ErrorStatus(UInt64 handle, Int32 Errcode, string msg)
        {
            string s = ((PrinterError)Errcode).ToString();
            try
            {
                System.Diagnostics.Debug.WriteLine(String.Format("handle: {0}, error={1}, msg='{2}', {3}", handle, Errcode, msg, s));

            }
            catch (Exception ex)
            {
                SetStatusMsg(false, ex.Message);
                //  throw;
            }
        }
        /// ************************************************************************************************
        /// <summary>
        /// SetStatusMsg
        /// </summary>
        /// <remarks>
        /// updates status msg
        /// <param name="bError">error</param>
        /// <param name="strMsg">msg</param>
        /// <Development> Implemented. </Development>                                         
        /// ************************************************************************************************
        private void SetStatusMsg(bool bError, string strMsg)
        {
            System.Diagnostics.Debug.WriteLine(strMsg);
        }

        class btPeer
        {
            public PeerInformation _pi
            {
                get; set;
            }
            public btPeer(PeerInformation p)
            {
                _pi = p;
            }
            public override string ToString()
            {
                return _pi.DisplayName;
            }
        }

        public async void LoadPrinters()
        {
            string PrintersFile = @"Assets\printer_profiles.JSON";
            StorageFolder InstallationFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            StorageFile file = await InstallationFolder.GetFileAsync(PrintersFile);
            Stream stream = await file.OpenStreamForReadAsync();
            StreamReader sr = new StreamReader(stream);
            String sJSON = sr.ReadToEnd();

            try {
                JObject jObject = JObject.Parse(sJSON);

                JToken outer = JToken.Parse(sJSON);
                JObject inner = outer["LINEPRINTERCONTROL"]["PRINTERS"].Value<JObject>();

                List<string> keys = inner.Properties().Select(p => p.Name).ToList();

                foreach (string k in keys)
                {
                    System.Diagnostics.Debug.WriteLine(k);
                    comboBox.Items.Add(k);
                }

            }catch(Exception ex){
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        private async Task fillBTpeers()
        {
            comboBoxBTpeers.Items.Clear();
            PeerFinder.AlternateIdentities["Bluetooth:Paired"] = "";
            var peerList = await PeerFinder.FindAllPeersAsync();
            if (peerList.Count > 0)
            {
                comboBoxBTpeers.IsEnabled = true;
                for (int i = 0; i < peerList.Count; i++)
                {
                    System.Diagnostics.Debug.WriteLine(string.Format("{0}: '{1}' '{2}'", i, peerList[i].DisplayName, peerList[i].HostName.ToString()));
                    comboBoxBTpeers.Items.Add(new btPeer(peerList[i]));//.DisplayName);
                }
                comboBoxBTpeers.SelectedIndex = 0;
            }
            else
            {
                comboBoxBTpeers.Items.Add("NO peers");
                comboBoxBTpeers.SelectedIndex = 0;
                comboBoxBTpeers.IsEnabled = false;
                await new MessageDialog("No paired BT devices found").ShowAsync();
                //MessageBox.Show("No active peers");
            }

        }
        private async void btSearch_Click(object sender, RoutedEventArgs e)
        {
            await fillBTpeers();
        }
    }
    //public class MessageBox
    //{
    //    public MessageBox(string s)
    //    {
    //        MessageDialog messageDialog = new MessageDialog(s);
    //        // Set the command to be invoked when escape is pressed
    //        messageDialog.CancelCommandIndex = 1;

    //        // Show the message dialog
    //        messageDialog.ShowAsync();
    //        messageDialog.Commands.Add(new UICommand("Close", new UICommandInvokedHandler(this.CommandInvokedHandler)));
    //    }
    //    private void CommandInvokedHandler(IUICommand command)
    //    {
    //        // Display message showing the label of the command that was invoked
    //        //rootPage.NotifyUser("The '" + command.Label + "' command has been selected.", NotifyType.StatusMessage);
    //    }
    //}
}
