using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Coronario2
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SymptomsCust : ContentPage
    {
        public SymptomsCust()
        {
            App m = ((App)App.Current);
            InitializeComponent();
            if (m.LanguageSelected == Coronario2.App.Lang.En)
            {
                l0.Text = "Customized Questionnaire of Symptoms:";
                btnBack.Text = "Back";
                btnNext.Text = "Next";
            }
            else
            {
                l0.Text = "Προσαρμοσμένο Ερωτηματολόγιο για Συμπτώματα:";
                btnBack.Text = "Πισω";
                btnNext.Text = "Επομενο";
            }
            int cursor = 0;
            while (cursor <= 15) 
            {
                if (m.q_arr[cursor].appear == true)
                {
                    if (s1.IsEnabled==false)
                    {
                        l1.Text = m.q_arr[cursor].Question;
                        s1.IsEnabled = true;
                        s1.IsVisible = true;
                    } else if (s2.IsEnabled == false)
                    {
                        l2.Text = m.q_arr[cursor].Question;
                        s2.IsEnabled = true;
                        s2.IsVisible = true;
                    }
                    else if(s3.IsEnabled == false)
                    {
                        l3.Text = m.q_arr[cursor].Question;
                        s3.IsEnabled = true;
                        s3.IsVisible = true;
                    }
                    else if(s4.IsEnabled == false)
                    {
                        l4.Text = m.q_arr[cursor].Question;
                        s4.IsEnabled = true;
                        s4.IsVisible = true;
                    }
                    else if(s5.IsEnabled == false)
                    {
                        l5.Text = m.q_arr[cursor].Question;
                        s5.IsEnabled = true;
                        s5.IsVisible = true;
                    }
                    else if(s6.IsEnabled == false)
                    {
                        l6.Text = m.q_arr[cursor].Question;
                        s6.IsEnabled = true;
                        s6.IsVisible = true;
                    }
                    else if(s7.IsEnabled == false)
                    {
                        l7.Text = m.q_arr[cursor].Question;
                        s7.IsEnabled = true;
                        s7.IsVisible = true;
                    }
                    else if(s8.IsEnabled == false)
                    {
                        l8.Text = m.q_arr[cursor].Question;
                        s8.IsEnabled = true;
                        s8.IsVisible = true;
                    }
                    else if(s9.IsEnabled == false)
                    {
                        l9.Text = m.q_arr[cursor].Question;
                        s9.IsEnabled = true;
                        s9.IsVisible = true;
                    }
                    else if(s10.IsEnabled == false)
                    {
                        l10.Text = m.q_arr[cursor].Question;
                        s10.IsEnabled = true;
                        s10.IsVisible = true;
                    }
                    else if(s11.IsEnabled == false)
                    {
                        l11.Text = m.q_arr[cursor].Question;
                        s11.IsEnabled = true;
                        s11.IsVisible = true;
                    }
                    else if(s12.IsEnabled == false)
                    {
                        l12.Text = m.q_arr[cursor].Question;
                        s12.IsEnabled = true;
                        s12.IsVisible = true;
                    }
                    else if(s13.IsEnabled == false)
                    {
                        l13.Text = m.q_arr[cursor].Question;
                        s13.IsEnabled = true;
                        s13.IsVisible = true;
                    }
                    else if(s14.IsEnabled == false)
                    {
                        l14.Text = m.q_arr[cursor].Question;
                        s14.IsEnabled = true;
                        s14.IsVisible = true;
                    }
                    else if(s15.IsEnabled == false)
                    {
                        l15.Text = m.q_arr[cursor].Question;
                        s15.IsEnabled = true;
                        s15.IsVisible = true;
                    }
                    else if(s16.IsEnabled == false)
                    {
                        l16.Text = m.q_arr[cursor].Question;
                        s16.IsEnabled = true;
                        s16.IsVisible = true;
                    }
                   
                }
                cursor++;
            }
        }

        async void OnNextClicked(object sender, EventArgs e)
        {

            var detailPage = new Geoloc();

            await Navigation.PushModalAsync(detailPage);
        }
        async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
            //NavigationPage nav = new NavigationPage(new MainPage());
        }

    }
}