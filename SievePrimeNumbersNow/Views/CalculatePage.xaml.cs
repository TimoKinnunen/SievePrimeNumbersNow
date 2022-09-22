using SievePrimeNumbersNow.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace SievePrimeNumbersNow.Views
{
    public sealed partial class CalculatePage : Page
    {
        CancellationTokenSource calculateCancellationTokenSource { get; set; }

        CancellationToken calculateCancellationToken { get; set; }

        readonly MainPage mainPage;

        int biggestPrimeNumber { get; set; }

        DispatcherTimer dispatcherTimer { get; set; }

        int countOfPrimenumberCandidates { get; set; } = 1;
        int countOfPrimenumbers { get; set; }

        int countOfMinutes { get; set; }

        List<OneStep> oneStepList { get; set; } = new List<OneStep>();

        public CalculatePage()
        {
            InitializeComponent();

            // do cache the state of the UI when suspending/navigating
            // this is necessary for EratosthenesSieveNow when navigating
            //NavigationCacheMode = NavigationCacheMode.Required;

            mainPage = MainPage.CurrentMainPage;
        }

        private async void DispatcherTimer_Tick(object sender, object e)
        {
            countOfMinutes++;
            RateTextBlock.Text = $"Calculation rate is {countOfPrimenumbers / countOfMinutes} primenumbers/minute ({countOfPrimenumbers}/{countOfMinutes}). Stepped through {countOfPrimenumberCandidates} primenumber candidates.";
            BiggestPrimeNumberTextBlock.Text = await App.PrimeNumberRepo.GetBiggestPrimeNumberAsStringAsync();
            var count = await App.PrimeNumberRepo.GetPrimeNumberItemsCountAsync();
            NumberOfPrimenumbersTextBlock.Text = count.ToString();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // code here
            if (await App.PrimeNumberRepo.GetPrimeNumberItemsCountAsync() == 0)
            {
                await App.PrimeNumberRepo.AddNewPrimeNumberItemAsync(2);
                await App.PrimeNumberRepo.AddNewPrimeNumberItemAsync(3);
                await App.PrimeNumberRepo.AddNewPrimeNumberItemAsync(5);
                oneStepList.Add(new OneStep { Id = 3, Value = 2 });
            }

            biggestPrimeNumber = int.Parse(await App.PrimeNumberRepo.GetBiggestPrimeNumberAsStringAsync());
            BiggestPrimeNumberTextBlock.Text = biggestPrimeNumber.ToString();
            int count = await App.PrimeNumberRepo.GetPrimeNumberItemsCountAsync();
            NumberOfPrimenumbersTextBlock.Text = count.ToString();

            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Interval = new TimeSpan(0, 1, 0);
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            // code here
        }


        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            // code here
            if (calculateCancellationToken.CanBeCanceled)
            {
                calculateCancellationTokenSource.Cancel();
            }
            if (calculateCancellationTokenSource != null)
            {
                calculateCancellationTokenSource.Dispose();
            }
            if (dispatcherTimer.IsEnabled)
            {
                dispatcherTimer.Stop();
            }
            // code here
        }

        #region MenuAppBarButton
        private void HomeAppBarButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            mainPage.GoToHomePage();
        }
        #endregion MenuAppBarButton

        private async void CalculateDataButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (CalculateDataButton.Content.ToString() == "Cancel")
            {
                if (calculateCancellationToken.CanBeCanceled)
                {
                    calculateCancellationTokenSource.Cancel();
                }
                if (dispatcherTimer.IsEnabled)
                {
                    dispatcherTimer.Stop();
                }
                CalculateDataButton.Content = "Calculate primenumbers";
                return;
            }

            if (CalculateDataButton.Content.ToString() == "Calculate primenumbers")
            {
                if (biggestPrimeNumber > 0)
                {
                    CalculateDataButton.Content = "Cancel";
                    mainPage.NotifyUser("Please wait or cancel. Calculation rate is shown after every passed minute.", NotifyType.StatusMessage);

                    StartCalculateDataProgressRing();

                    countOfPrimenumberCandidates = 0;
                    countOfPrimenumbers = 0;
                    countOfMinutes = 0;
                    dispatcherTimer.Start();

                    try
                    {
                        calculateCancellationTokenSource = new CancellationTokenSource();
                        calculateCancellationToken = calculateCancellationTokenSource.Token;

                        biggestPrimeNumber = int.Parse(await App.PrimeNumberRepo.GetBiggestPrimeNumberAsStringAsync());

                        int id = GetIdOfPrimenumber(biggestPrimeNumber);

                        //add cancellationToken to this task
                        await Task.Run(async () => await CalculateDataAsync(id), calculateCancellationToken);
                    }
                    catch (TaskCanceledException)
                    {
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            mainPage.NotifyUser("Task was cancelled.", NotifyType.ErrorMessage);
                        });
                    }
                    catch (Exception ex)
                    {
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            mainPage.NotifyUser(ex.Message, NotifyType.ErrorMessage);
                        });
                    }
                    finally
                    {
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            CalculateDataButton.Content = "Calculate primenumbers";

                            dispatcherTimer.Stop();
                            RateTextBlock.Text = string.Empty;

                            StopCalculateDataProgressRing();
                        });
                    }
                }
            }
        }

        private async Task CalculateDataAsync(int id)
        {
            while (!calculateCancellationTokenSource.IsCancellationRequested)
            {
                if (calculateCancellationTokenSource.IsCancellationRequested)
                {
                    break;
                }

                id++;
                countOfPrimenumberCandidates += 2; //using only odd numbers here but add 2 meaning even and odd numbers

                foreach (var oneStep in oneStepList)
                {
                    oneStep.Value = oneStep.Value <= 0 ? oneStep.Id - 1 : oneStep.Value - 1;
                }

                if (oneStepList.Any(l => l.Value == 0))
                {
                    continue; //continue while
                }

                int primeNumberCandidate = GetPrimenumberOfId(id);

                #region filter primeNumberCandidate on last digit
                string lastDigit = primeNumberCandidate.ToString().Substring(primeNumberCandidate.ToString().Length - 1, 1);

                switch (lastDigit)
                {
                    case "1": //possible primenumber
                        break;
                    case "3": //possible primenumber
                        break;
                    //leave 5 out
                    //case "5": //possible primenumber
                    //    break;
                    case "7": //possible primenumber
                        break;
                    case "9": //possible primenumber
                        break;
                    default:
                        continue; //continue while
                }
                #endregion filter primeNumberCandidate on last digit

                int startCountingDown = GetIdOfPrimenumber(primeNumberCandidate * primeNumberCandidate) - id;

                await App.PrimeNumberRepo.AddNewPrimeNumberItemAsync(primeNumberCandidate);

                if (!oneStepList.Any(l => l.Id == primeNumberCandidate))
                {
                    oneStepList.Add(new OneStep { Id = primeNumberCandidate, Value = startCountingDown });
                }

                countOfPrimenumbers++;

                if (countOfPrimenumbers % 100 == 0)
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        mainPage.NotifyUser($"Added new primenumber {primeNumberCandidate} to database table.", NotifyType.StatusMessage);
                    });
                }
            }
        }

        int GetPrimenumberOfId(int i)
        {
            return i * 2 + 1;
        }

        int GetIdOfPrimenumber(int biggestPrimenumber)
        {
            return (biggestPrimenumber - 1) / 2;
        }

        private void StartCalculateDataProgressRing()
        {
            CalculateDataProgressRing.IsActive = true;
            CalculateDataProgressRing.Visibility = Visibility.Visible;
        }

        private void StopCalculateDataProgressRing()
        {
            CalculateDataProgressRing.IsActive = false;
            CalculateDataProgressRing.Visibility = Visibility.Collapsed;
        }
    }
}
