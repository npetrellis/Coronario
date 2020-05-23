using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;


using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Xamarin.Essentials;

using Plugin.FilePicker;
using SkiaSharp;
using SkiaSharp.Views.Forms;



namespace Coronario2
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Sound : ContentPage
    {
        byte[] filecontents;
        SKBitmap bitmap;
        int curr_train;
        double[] trainval;
        double[] sampleval;
        double[] samplemax;
        double cur_mean;
        PCA pca;
        
        string tmptrain;
        int tottrain;
        int frames;
        int N;
        double[,] spectrum;

        string[] classnames;
        double[,] classval;

        public Sound()
        {
            frames = 0;
            N = 1024;
            int N1 = 1080;
            SKColor wh = new SKColor();
            wh.WithAlpha(0xFF);
            wh.WithBlue(0xFF);
            wh.WithRed(0xFF);
            wh.WithGreen(0xFF);
            bitmap = new SKBitmap(1920, 1080);
            int col, row;
            for (col = 0; col < 1920; col++)
                for (row = 0; row < N1; row++)
                        bitmap.SetPixel(col, row, wh);
            canvasView = new SKCanvasView();
            canvasView.PaintSurface += OnCanvasViewPaintSurface;
            Content = canvasView;

            InitializeComponent();

            curr_train = 0;
            tottrain = 0;

            classnames= new string[20]; // maximum 20 classes
            

            pca = new PCA();

            App m = ((App)App.Current);
            if (m.LanguageSelected == Coronario2.App.Lang.En)
            {
                l0.Text = "Sound Processor";
                btnOpen.Text = "Open";
                btnPlay.Text = "Play";
                btnAnalyze.Text = "Analyze";
                l1.Text = "Cough";
                l2.Text = "Respiratory";
                l3.Text = "Training";
                l4.Text = "Tr. Samples";
                l5.Text = "FFT Size";
                l6.Text = "Subsampling (f/?)";
                l7.Text = "Param1";
                l8.Text = "Param2";

                editorResults.Text = "Analysis Results";
                btnDraw.Text = "Spectrum Draw";
                btnClassify.Text = "Classify";
                btnBack.Text = "Back";
                btnNext.Text = "Next";
            }
            else
            {
                l0.Text = "Επεξεργαστής Ήχου";
                btnOpen.Text = "ΑΝΟΙΓΜΑ";
                btnPlay.Text = "ΑΝΑΠΑΡΑΓΩΓΗ";
                btnAnalyze.Text = "ΑΝΑΛΥΣΗ";
                l1.Text = "Βήχας";
                l2.Text = "Αναπνοή";
                l3.Text = "Εκπαίδευση";
                l4.Text = "Δείγματα Εκπ.";
                l5.Text = "Διάσταση FFT";
                l6.Text = "Υποδειγματοληψία (f/?)";
                l7.Text = "Παράμετρος 1";
                l8.Text = "Παράμετρος 2";
                editorResults.Text = "Αποτελέσματα Ανάλυσης";
                btnDraw.Text = "ΣΧΕΔΙΑΣΗ ΦΑΣΜΑΤΟΣ";
                btnClassify.Text = "ΤΑΞΙΝΟΜΗΣΗ";
                btnBack.Text = "ΠΙΣΩ";
                btnNext.Text = "ΕΠΟΜΕΝΟ";
            }
            tmptrain = l4.Text;
            entryParam1.IsEnabled = false;
            entryParam2.IsEnabled = false;
            pickerAnalysis.SelectedIndex = 0;
        }

     
        
        private async void OnNextClicked(object sender, EventArgs e)
        {
            var detailPage = new Results();

            await Navigation.PushModalAsync(detailPage);
        }
        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
            //NavigationPage nav = new NavigationPage(new MainPage());
        }

        private SKColor colorshift(byte level)
        {
            int lev = (int) ((double)level*2.5);
            if (lev > 255) lev = 255;
            
            SKColor colorval = new SKColor((byte)lev, (byte)lev, (byte)(255-(int)lev),0xFF);
            
           
            return colorval;
        }

        private void OnIndexChanged(object sender, EventArgs e)
        {
            if ((pickerAnalysis.SelectedIndex == 0) || (pickerAnalysis.SelectedIndex == 1))
            {
                entryParam1.IsEnabled = false;
                entryParam2.IsEnabled = false;
            } else if ((pickerAnalysis.SelectedIndex == 2) || (pickerAnalysis.SelectedIndex == 3))
            {
                // PCA
                entryParam1.IsEnabled = true;
                entryParam2.IsEnabled = true;
                entryParam2.Text = "5";
                entryParam1.Text = "128";
                l7.Text = "Segments";
                l8.Text = "Components";
                
            }
            else if ((pickerAnalysis.SelectedIndex == 4) || (pickerAnalysis.SelectedIndex == 5))
            {
                // Moving Average
                entryParam1.IsEnabled = true;
                l7.Text = "Window";
                entryParam1.Text = "7";
                entryParam2.IsEnabled = false;

            }
        }

        private void av_win(int[] data, int N, int mawind, ref double[] pm_out)
        {
            // If N is even e.g 2 intN will be +/-2 if it is odd e.g. 5 it will be again +/-2

            int Wfloor = mawind / 2;
            int DMWfloor = 2 * Wfloor + 1;
            for (int i = 0; i < Wfloor; i++) pm_out[i] = data[i];
            for(int i = Wfloor; i < N - Wfloor; i++)
            {
                double sum = 0;
                for (int j = i - Wfloor; j <= i + Wfloor;j++) sum += data[j];
                pm_out[i] = sum / DMWfloor;
            }
            for (int i = N-Wfloor; i < N; i++) pm_out[i] = data[i];
        }

        private void OnAnalyze(object sender, EventArgs e)
        {
            
            btnAnalyze.IsEnabled = false;
            if (filecontents != null)
            {
                // Test FFT here first
                Spectrum sp = new Spectrum();
                
                N = Int32.Parse(entryFFTSize.Text);
                classval = new double[20,N];
                int subsampling = 4;        // Freq=44100Hz/subsampling
                int maxseg = 10;
                frames = (int)(filecontents.Length / (subsampling*N));
                byte[] tmp = new byte[N];
                //double[,] sp_re = new double[frames, N];
                //double[,] sp_im = new double[frames, N];
                spectrum = new double[frames, N];
                double[] tmp_re = new double[N];
                double[] tmp_im = new double[N];
                //double[,] sumdre = new double[maxseg,N];
                //double[,] sumdim = new double[maxseg,N];
                double[,] sumd = new double[maxseg, N];
                double[,] maxd = new double[maxseg, N];
                int[] tmp_sp = new int[N];

                

                int subsampl;
                subsampl = Int32.Parse(entrySubsampling.Text);
                

                int trsamples;
                    trsamples = Int32.Parse(entrySamples.Text);
                

                if (chkTraining.IsChecked==true)
                {
                    // assume it is lower than trsamples .Checked below
                    curr_train++;
                    string addtmp = "(" + curr_train.ToString() + ":" + trsamples.ToString() + ")";
                    l4.Text = tmptrain + addtmp;
                }

                if (trainval == null)
                {
                    trainval = new double[N];
                    for (int i = 0; i < N; i++) trainval[i] = 0;
                }
                if (sampleval == null)
                {
                    sampleval = new double[N];
                    for (int i = 0; i < N; i++) sampleval[i] = 0;
                }
                if (samplemax == null)
                {
                    samplemax = new double[N];
                    for (int i = 0; i < N; i++) samplemax[i] = 0;
                }

                int frame_no = 0;
                int seg_no = 0;
                int col, row;
                int pos = 0;
                double abssum = 0;
                string[] newrow = new string[2];
                for (int k=0;k<maxseg;k++)
                for (int j = 0; j < N; j++)
                {
                        //sumdre[k,j] = sumdim[k,j]=0;
                        sumd[k, j] = 0;
                        maxd[k, j] = -9999;
                    }
                while (frame_no < frames)
                {
                    newrow[0] = "";
                    // more frames to FFT!!
                    for (int i = 0; i < N; i++)
                    {      // copy N bytes to tmp
                        tmp[i] = filecontents[pos];
                        pos += subsampl;
                    }
                    sp.DFT_realIn(tmp, N, ref tmp_re, ref tmp_im, ref tmp_sp);
                    abssum = sp.DFT_check(tmp_re, tmp_im, N);
                    if (abssum < Math.Pow(10, -20))
                    {
                        // almost zero ignore subsequent zero frames and increase seg_no
                        while (abssum < Math.Pow(10, -20))
                        {
                            for (int i = 0; i < N; i++)         // copy N bytes to tmp
                                tmp[i] = filecontents[pos++];
                            sp.DFT_realIn(tmp, N, ref tmp_re, ref tmp_im, ref tmp_sp);
                            abssum = sp.DFT_check(tmp_re, tmp_im, N);
                            frame_no++;
                        }
                        seg_no++;
                        if (seg_no >= maxseg) break;    // up to maxseg frames are checked
                    }
                    else
                        if (seg_no == 0) seg_no = 1;

                    double[] pm_out = new double[N];
                    if ((pickerAnalysis.SelectedIndex == 2) || (pickerAnalysis.SelectedIndex == 3)) { 

                        // PCA
                       
                        int Seg=Int32.Parse(entryParam1.Text);
                        int Comps = Int32.Parse(entryParam2.Text);
                        pca.pca_extract(tmp_sp, N, Seg, Comps, ref pm_out);
                        
                    } else if ((pickerAnalysis.SelectedIndex == 4) || (pickerAnalysis.SelectedIndex == 5))
                    {

                        // Mov. Average

                        int win = Int32.Parse(entryParam1.Text);
                        av_win(tmp_sp, N, win, ref pm_out);
                    }

                    if ((pickerAnalysis.SelectedIndex == 0) || (pickerAnalysis.SelectedIndex == 1))
                    {
                        for (int i = 0; i < N; i++)
                        {
                            //sp_re[frame_no, i] = tmp_re[i];
                            //sp_im[frame_no, i] = tmp_im[i];
                            //abssum += Math.Abs(tmp_re[i]) + Math.Abs(tmp_im[i]);

                            spectrum[frame_no, i] = tmp_sp[i];
                            sumd[seg_no - 1, i] += tmp_sp[i];
                            if (maxd[seg_no - 1, i] < tmp_sp[i]) maxd[seg_no - 1, i] = tmp_sp[i];

                            //sumdim[seg_no-1,i] += tmp_im[i];
                            //sumdre[seg_no-1,i] += tmp_re[i];
                            //newrow[0] += tmp_re[i].ToString()+",";
                        }
                    } else if (pickerAnalysis.SelectedIndex >= 2)
                    {
                        // PCA or Mov. Average
                        for (int i = 0; i < N; i++)
                        {
                            //sp_re[frame_no, i] = tmp_re[i];
                            //sp_im[frame_no, i] = tmp_im[i];
                            //abssum += Math.Abs(tmp_re[i]) + Math.Abs(tmp_im[i]);

                            spectrum[frame_no, i] = pm_out[i];
                            sumd[seg_no - 1, i] += pm_out[i];
                            if (maxd[seg_no - 1, i] < pm_out[i]) maxd[seg_no - 1, i] = pm_out[i];

                            //sumdim[seg_no-1,i] += tmp_im[i];
                            //sumdre[seg_no-1,i] += tmp_re[i];
                            //newrow[0] += tmp_re[i].ToString()+",";
                        }
                    }
                    
                    // NPETDEBUG
                    //File.AppendAllLines("C:\\users\\nikos\\source\\repos\\Coronario21\\Coronario2\\Coronario2.UWP\\bin\\x86\\Debug\\AppX\\FFT_magn.csv", newrow);
                    frame_no++;
                    
                }


                // Here either use tmp_sp as the original output or apply PCA or moving average 



                newrow[0] = "";
                    newrow[1] = "";
                    for (int k = 0; k < seg_no; k++)
                    {
                    double sum = 0;
                        for (int j = 0; j < N; j++)
                        {
                            //newrow[0] += sumdre[k, j].ToString() + ",";
                            sampleval[j] = sumd[k, j] / frame_no;
                            samplemax[j] = maxd[k, j];
                            sum += sampleval[j];
                            //newrow[0] += sampleval[j].ToString() + ",";
                            //newrow[1] += maxd[k, j].ToString() + ",";
                            //newrow[1] += sumdim[k, j].ToString() + ",";

                            if ((pickerAnalysis.SelectedIndex == 1) || 
                                (pickerAnalysis.SelectedIndex == 3) || (pickerAnalysis.SelectedIndex == 5))
                            {
                                newrow[0] += samplemax[j].ToString() + ",";
                                if (chkTraining.IsChecked == true)
                                    trainval[j] += samplemax[j];
                            }
                            else
                            {
                                newrow[0] += sampleval[j].ToString() + ",";
                                if (chkTraining.IsChecked == true)
                                    trainval[j] += sampleval[j];
                            }

                            
                        };
                        cur_mean = sum/N;

                        tottrain++;

                        newrow[0] += "$$$,";
                        //newrow[1] += "$$$,";
                    }
                if (chkTraining.IsChecked == true)
                {
                    if (curr_train + 1 > trsamples)
                    {
                        // stop training. Show training results. Deactivate checkbox.k
                        chkTraining.IsChecked = false;

                        for (int j = 0; j < N; j++)
                        {
                            //newrow[0] += sumdre[k, j].ToString() + ",";
                            newrow[1] += (trainval[j] / tottrain).ToString() + ",";
                        }
                        editorResults.IsVisible = false;
                        editorResults.IsEnabled = false;

                        editorResults.IsEnabled = true;
                        editorResults.IsVisible = true;
                        editorResults.Text = newrow[1];
                    }
                }
                else
                {
                    editorResults.IsVisible = false;
                    editorResults.IsEnabled = false;

                    editorResults.IsEnabled = true;
                    editorResults.IsVisible = true;
                    editorResults.Text = newrow[0];
                }

                    
                


               
                btnAnalyze.IsEnabled = true;
               
            }
        }

        private async void OnClassifyClicked(object sender, EventArgs e)
        {
            // Dynamically Open the classes file
            string[] classes ;

            var file = await CrossFilePicker.Current.PickFile();
            if (file != null)
            {
                //string fileName = "/storage/0F01-0F02/Android/Data/com.companyname.coronario2/cache/testcough.wav"
                //"/content:/com.android.externalstorage.documents/document/0F01-0F02%3AAndroid%2Fdata%2Fcom.companyname.audioplayer%2Fcache%2Ftestcough.wav


                // Parse string delete up to /0F01 and replace %2F with /
                string str = file.FilePath;
                string str1 = str.Replace("%2F", "/");
                string str2 = str1.Replace("%3A", "/");
                int found = str2.IndexOf("/1E17");
                string str3 = "";
                if (found > 0)
                    str3 = "/storage" + str2.Substring(found);


                //NPETDEBUG If UWP keep str
                //filecontents = File.ReadAllBytes(str3);
                
                btnClassify.Text = file.FileName;
                classes = File.ReadAllLines(str);
                // now each string line is a number from a class starting with the class name in $name format
                int p = 0;
                int indx=0;
                int classindx = -1;
                int classp = 0;
                while (p < classes.Length)
                {
                    indx = classes[p].IndexOf("$");
                    if (indx<0)
                    {
                        classval[classindx, classp++] = Double.Parse(classes[p++]);
                    }
                    else
                    {
                        classnames[++classindx] = classes[p].Substring(indx+1); // Copy the name of the class
                        if ((classp!=N) &&(classindx!=0))
                        {
                            editorResults.IsVisible = false;
                            editorResults.IsEnabled = false;

                            editorResults.IsEnabled = true;
                            editorResults.IsVisible = true;
                            editorResults.Text = "ERROR class size from file=" + classp.ToString() + " while N=" + N.ToString();
                            return;
                        }
                        classp = 0;
                        p++;
                    }
                }

                // Now classnames and classval have been populated. The number of classes read are classindx
                //if ((pickerAnalysis.SelectedIndex==0) || (pickerAnalysis.SelectedIndex == 1)) // Pearson (addition or max)
                //{
                    double[] similarities = new double[classindx+1];

                    // Pearson Correlation Similarity
                    for (p = 0; p <= classindx; p++)
                    {
                        // Now class p is examined for similarity
                        // cur_mean and sampleval array are the examined sample
                        // find mean of the referenced class
                        double ref_mean = 0;
                        double sum1 = 0;
                        double sum2 = 0;
                        double sum3 = 0;
                        for (int i = 0; i < N; i++) 
                            ref_mean += classval[p, i]; 
                        
                        ref_mean = ref_mean / N;
                        if ((pickerAnalysis.SelectedIndex == 0) || (pickerAnalysis.SelectedIndex == 2) || (pickerAnalysis.SelectedIndex == 4))
                        {
                            for (int i = 0; i < N; i++)
                            {
                                sum1 += (classval[p, i] - ref_mean) * (sampleval[i] - cur_mean);
                                sum2 += (classval[p, i] - ref_mean) * (classval[p, i] - ref_mean);
                                sum3 += (sampleval[i] - cur_mean) * (sampleval[i] - cur_mean);
                            }
                        } else
                        {
                            for (int i = 0; i < N; i++)
                            {
                                sum1 += (classval[p, i] - ref_mean) * (samplemax[i] - cur_mean);
                                sum2 += (classval[p, i] - ref_mean) * (classval[p, i] - ref_mean);
                                sum3 += (samplemax[i] - cur_mean) * (samplemax[i] - cur_mean);
                            }
                        }
                        similarities[p] = sum1 / (Math.Sqrt(sum2*sum3));


                    }

                    // At this point similarities must contain the rank of each class!!
                    int selected_index = 0;
                    string selected_classname;
                    double maxsim = -10;
                    for (p = 0; p <= classindx; p++) if (similarities[p] > maxsim) { selected_index = p; maxsim = similarities[p]; }
                    selected_classname = classnames[selected_index];

                    string erstr;
                    erstr= "Recognized as: " + selected_classname;

                    
                    editorResults.IsVisible = false;
                    editorResults.IsEnabled = false;
                    
                    editorResults.IsEnabled = true;
                    editorResults.IsVisible = true;

                    editorResults.Text = erstr;

                //}
            }
            else classes = null;
        }

            private void OnDrawClicked(object sender, EventArgs e)
        {

            // create image to visualize spectrum
            // vertical dimension is FFT length e.g. 1024
            // the other dimension depends on the duration
            // Assume resolution 1920X1080
            // 1080 for the 1024 FFT points
            // 1920 / 10 = 192 maximum frames. If the duration is higher the last 192 are displayed
            //byte[,] spectrum_img = new byte[1920, 1080];
            //byte[] spectrum_img = new byte[1920 * 1080];

            SKColor testc = new SKColor();
            byte testv;
            int pos = 0; // shows the frame number
            if (frames >= 192) pos = frames - 191; // display the last 192 frames
            for (int col = 0; col < 1920; col++)
                for (int row = 0; row < N; row++)
                {
                    int frame_no = (int)Math.Floor((double)(col / 10)) + pos;
                    if (frame_no < frames)
                    {
                        testv = (byte)spectrum[frame_no, row];

                        testc = colorshift(testv);
                        bitmap.SetPixel(col, row, testc);
                        testc = bitmap.GetPixel(col, row);
                    }
                    //spectrum_img[col*1080+ row] = (byte) spectrum[frame_no,row];
                }
            // SKCanvasView canvasView = new SKCanvasView();
            canvasView.PaintSurface += OnCanvasViewPaintSurface;
            Content = canvasView;
        }
        void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs args)
        {
            SKImageInfo info = args.Info;
            SKSurface surface = args.Surface;
            SKCanvas canvas = surface.Canvas;

            canvas.Clear();

           canvas.DrawBitmap(bitmap, 50, 50);
        }

        private async void OnOpen(object sender, EventArgs e)
        {
            var file = await CrossFilePicker.Current.PickFile();
            if (file != null)
            {
                //string fileName = "/storage/0F01-0F02/Android/Data/com.companyname.coronario2/cache/testcough.wav"
                //"/content:/com.android.externalstorage.documents/document/0F01-0F02%3AAndroid%2Fdata%2Fcom.companyname.audioplayer%2Fcache%2Ftestcough.wav


                // Parse string delete up to /0F01 and replace %2F with /
                string str = file.FilePath;
                string str1 = str.Replace("%2F", "/");
                string str2 = str1.Replace("%3A", "/");
                int found = str2.IndexOf("/1E17");
                string str3="";
                if (found>0)
                    str3 = "/storage"+str2.Substring(found);


                //NPETDEBUG If UWP keep str
                //filecontents = File.ReadAllBytes(str3);
                filecontents = File.ReadAllBytes(str);
                btnOpen.Text = file.FileName;

            }
            else filecontents = null;
        }
        private void OnPlay(object sender, EventArgs e)
        {
            btnPlay.IsEnabled = false;
            if (filecontents != null)
            {
                var audio = Plugin.SimpleAudioPlayer.CrossSimpleAudioPlayer.Current;
                Stream strm = new MemoryStream(filecontents);
                audio.Load(strm);
                audio.Play();
            }
            btnPlay.IsEnabled = true;
        }
        private void OnChangedRespiratory(object sender, EventArgs e)
        {
            if (chkRespiratory.IsChecked==true)
            {
                chkCough.IsChecked = false;
            } else
            {
                chkCough.IsChecked = true;
            }
        }
        private void OnChangedCough(object sender, EventArgs e)
        {
            if (chkCough.IsChecked == true)
            {
                chkRespiratory.IsChecked = false;
            } else
            {
                chkRespiratory.IsChecked = true;
            }
        }
    }

    
}