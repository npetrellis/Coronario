using Microsoft.Azure.Devices.Client;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Hadoop.Avro.Container;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Text;
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

using Plugin.FilePicker;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace CoronarioSupervisor
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    
    public sealed partial class MainPage : Page
    {
        string filecontents;
        bool OpenMP;
        bool connstat = false;
        bool english = true;
        private static DeviceClient s_deviceClient;
        private string s_myDeviceId ;
        private string s_iotHubUri ;
        // This is the primary key for the device. This is in the portal. 
        // Find your IoT hub in the portal > IoT devices > select your device > copy the key. 
        private string s_deviceKey ;


        public MainPage()
        {
            OpenMP = true;
            this.InitializeComponent();
        }

        private void OnEnglish(object sender, RoutedEventArgs e)
        {
            if (radioButtonEnglish.IsChecked == true)
            {
                radioButtonGreek.IsChecked = false;
                english = true;
                textTitle.Text="Coronario Supervisor Demo";
                buttonViewSensorData.Content = "View Sensor Data";
                textSensorId.Text = "Sensor Id:";
                buttonSendMessage.Content = "Send Message:";
                buttonFilePicker.Content = "Open Medical Protocol File";
                buttonQuit.Content = "Quit";
                buttonConnect.Content = "Connect";
                textConnDetails.Text = "Connection Details, Status: Unconnected";
                textDeviceId.Text = "DeviceId:";
                editDeviceId.Text = "CoronarioSensors-Test-Device";
                editIoTHub.Text = "CoronarioTestHub2722.azure-devices.net";
                textIoTHub.Text = "IoT Hub:";
            } 
        }
        private void OnGreek(object sender, RoutedEventArgs e)
        {
            if (radioButtonGreek.IsChecked == true)
            {
                radioButtonEnglish.IsChecked = false;
                english = false;
                textTitle.Text="Coronario Supervisor Demo";
                buttonViewSensorData.Content = "Επισκόπιση Τιμών Αισθητήρων";
                textSensorId.Text = "Ταυτότητα Αισθητήρα:";
                buttonSendMessage.Content = "Αποστολή Μηνύματος:";
                buttonFilePicker.Content = "Άνοιγμα Ιατρικού Πρωτοκόλλου";
                buttonQuit.Content = "Έξοδος";
                buttonConnect.Content = "Σύνδεση";
                textConnDetails.Text = "Λεπτομέρειες Σύνδεσης, Κατάσταση: Μη Συνδεδεμένο";
                textDeviceId.Text = "DeviceId:";
                editDeviceId.Text = "CoronarioSensors-Test-Device";
                editIoTHub.Text = "CoronarioTestHub2722.azure-devices.net";
                textIoTHub.Text = "IoT Hub:";
            }
        }

        private void OnViewSensorData(object sender, RoutedEventArgs e)
        {
            string accum = "";
            string parsed, unparsed;
            int count;

            byte[] fullmsg = new byte[102400];
            int idx = 0;
            
            // Get a connection string to our Azure Storage account.
            string connectionString = Constants.StorageConnection;

            // Get a reference to a container named "sample-container" and then create it
            BlobContainerClient container = new BlobContainerClient(connectionString, "coronarioresults");
            // await container.CreateAsync();
            try
            {
                // Get a reference to a blob named "sample-file"
                //BlobClient blob = container.GetBlobClient("https://coronariostorage2722.blob.core.windows.net/coronarioresults/CoronarioTestHub2722/01/2020/04/19/23/32");
                BlobClient blob = container.GetBlobClient("CoronarioTestHub2722/01/2020/04/19/23/32");

                // First upload something the blob so we have something to download
                //await blob.UploadAsync(File.OpenRead(originalPath));

                // Download the blob's contents and save it to a file
                var download = blob.DownloadAsync();
                var contents = download.Result.Value.Content;
                
                Encoding enc = new UTF8Encoding(); //(false, true, true);
                
                
                using (contents)
                {
                    count = 0;
                    do
                    {
                        byte[] buf = new byte[9216];
                        
                        count = contents.Read(buf, 0, 9216);
                        buf.CopyTo(fullmsg, idx); // ToString(); //enc.GetString(buf);
                        idx += count;
                        
                    } while (contents.CanRead && count > 0);

                }
                
                byte[] finbuf = new byte[idx];
                count = 0;
                int newcount = 0;
                while (count < idx)
                {
                    if (fullmsg[count] != '\0')
                        finbuf[newcount++] = fullmsg[count++];
                    else
                        count++;
                }
                unparsed= enc.GetString(finbuf);
                accum = "";
                count = 0;
                int indexOfSteam = 0;
                string nextunparsed=unparsed;
                while (count < idx)
                {
                    indexOfSteam = nextunparsed.IndexOf("deviceId");

                    if (indexOfSteam >= 0)
                    {
                        count += indexOfSteam;
                        parsed = nextunparsed.Substring(indexOfSteam); //, unparsed.Length - indexOfSteam);
                        indexOfSteam = parsed.IndexOf("}");
                        count += indexOfSteam;
                        nextunparsed = parsed.Substring(indexOfSteam + 1); //, parsed.Length - indexOfSteam - 1);
                        parsed = "{\""+ parsed.Remove(indexOfSteam, parsed.Length - indexOfSteam) +"}\n";
                        //string value2 = enc.GetString(buffer);
                        //dynamic d1 = JObject.Parse(parsed);
                        var d1 = JObject.Parse(parsed); // parse as array 
                        string analyzed = "";
                                             
                        foreach (KeyValuePair<String, JToken> app in d1)
                            {
                                var appName = app.Key;
                                var description = (String)app.Value;
                                analyzed += appName+": " + description+", ";
                            }
                        analyzed += "\n";

                        accum += analyzed;
                        //accum += parsed;
                    }
                    else break;
                }
                textBlock.Text = accum;

                //using (FileStream file = File.OpenWrite("C:\\Users\nikos\\source\\repos\\CoronarioSupervisor\\CoronarioSupervisor\\bin\\x86\\Debug\\AppX\\coronariotest.txt"))
                //{
                //var contents= await download.Content.ToString();
                //}


            }
            finally
            {
                // DELETED THE REAL RESOURCE!!! COMMENT IT!!!!
                // Clean up after the test when we're finished
                // await container.DeleteAsync();
            }
        }
        private async void OnSendMsg(object sender, RoutedEventArgs e)
        {
            string msg;
            editSendMsg.Document.GetText(Windows.UI.Text.TextGetOptions.None, out msg); //.ToString() // .LoadFromStream(Windows.UI.Text.TextSetOptions.FormatRtf, randAccStream); //DataContext.ToString()
            var MP = new
            {
                deviceId = s_myDeviceId,
                mpstring = msg
            };

            var serializedMPString = JsonConvert.SerializeObject(MP);

            var message = new Message(Encoding.UTF32.GetBytes(serializedMPString));

            //Add one property to the message.
            message.Properties.Add("level", "msg");

            // Submit the message to the hub.
            await s_deviceClient.SendEventAsync(message);
        }
        private async void OnMPFclick(object sender, RoutedEventArgs e)
        {
            if (OpenMP) {
                var installDirectory = Windows.ApplicationModel.Package.Current.InstalledLocation;
                editProtocolFileName.PlaceholderText = "Access Rights to: " + installDirectory.Path;
                var file = await CrossFilePicker.Current.PickFile();
                if (file != null)
                {
                    
                    filecontents = File.ReadAllText(file.FilePath);
                    editProtocolFileName.PlaceholderText = file.FilePath;
                    textBlock.Text = filecontents;
                }
                else filecontents = null;
                OpenMP = false;
                if (english==true)
                    buttonFilePicker.Content = "Send Medical Protocol";
                else
                    buttonFilePicker.Content = "Αποστολή Ιατρικού Πρωτοκόλλου";
            } else
            {
                // Upload Medical Protocol to Cloud:
                var MP = new
                {
                    deviceId = s_myDeviceId,
                    mpstring = filecontents
                };
                
                var serializedMPString = JsonConvert.SerializeObject(MP);

                var message = new Message(Encoding.UTF32.GetBytes(serializedMPString));

                //Add one property to the message.
                message.Properties.Add("level", "mp");

                // Submit the message to the hub.
                await s_deviceClient.SendEventAsync(message);
                
                // end upload
                OpenMP = true;
                if (english == true)
                    buttonFilePicker.Content = "Open Medical Protocol File";
                else
                    buttonFilePicker.Content = "Άνοιγμα Ιατρικού Πρωτοκόλλου";
                
            }

        }

        private void OnConnect(object sender, RoutedEventArgs e)
        {
            s_myDeviceId=editDeviceId.Text;
            s_iotHubUri=editIoTHub.Text;
            // This is the primary key for the device. This is in the portal. 
            // Find your IoT hub in the portal > IoT devices > select your device > copy the key. 
            s_deviceKey= passwordDeviceKey.Password;
            if (connstat == false)
            {
                s_deviceClient = DeviceClient.Create(s_iotHubUri, new DeviceAuthenticationWithRegistrySymmetricKey(s_myDeviceId, s_deviceKey), TransportType.Mqtt);
                if (s_deviceClient == null)
                {
                    connstat = false;
                    if (english==true)
                        textConnDetails.Text = "Connection Details, Status: Unconnected";
                    else
                        textConnDetails.Text = "Λεπτομέρειες Σύνδεσης, Κατάσταση: Μη Συνδεδεμένο";
                    buttonConnect.Content = "Connect";
                }
                else
                {
                    connstat = true;
                    if (english == true)
                    {
                        textConnDetails.Text = "Connection Details, Status: Connected";
                        buttonConnect.Content = "Disconnect";
                    }
                    else
                    {
                        textConnDetails.Text = "Λεπτομέρειες Σύνδεσης, Κατάσταση: Συνδεδεμένο";
                        buttonConnect.Content = "Αποσύνδεση";
                    }
                    
                    
                }
            } else
            {
                // Disconnect
                s_deviceClient.CloseAsync();
                connstat = false;
                if (english == true)
                {
                    textConnDetails.Text = "Connection Details, Status: Unconnected";
                    buttonConnect.Content = "Connect";
                }
                else
                {
                    textConnDetails.Text = "Λεπτομέρειες Σύνδεσης, Κατάσταση: Μη Συνδεδεμένο";
                    buttonConnect.Content = "Σύνδεση";
                }
                
            }
        }
        private void OnQuit(object sender, RoutedEventArgs e)
        {
            // Disconnect
          
            if (s_deviceClient!=null)
                s_deviceClient.CloseAsync();
            Application.Current.Exit(); 
        }

    }
}
