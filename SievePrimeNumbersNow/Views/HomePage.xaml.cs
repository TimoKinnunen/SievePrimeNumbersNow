﻿using Newtonsoft.Json;
using SievePrimeNumbersNow.Data;
using SievePrimeNumbersNow.Helpers;
using SievePrimeNumbersNow.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Provider;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace SievePrimeNumbersNow.Views
{
    public sealed partial class HomePage : Page
    {
        private readonly MainPage mainPage;

        private readonly ObservableCollection<PrimeNumberItem> primeNumberItemsObservableCollection;

        private List<PrimeNumberItem> primeNumberItemsList;

        public HomePage()
        {
            InitializeComponent();

            Loaded += HomePage_Loaded;

            primeNumberItemsObservableCollection = new ObservableCollection<PrimeNumberItem>();

            mainPage = MainPage.CurrentMainPage;
        }

        private async void HomePage_Loaded(object sender, RoutedEventArgs e)
        {
            if (await App.PrimeNumberRepo.GetPrimeNumberItemsCountAsync() == 0)
            {
                await App.PrimeNumberRepo.AddNewPrimeNumberItemAsync(2);
                await App.PrimeNumberRepo.AddNewPrimeNumberItemAsync(3);
                await App.PrimeNumberRepo.AddNewPrimeNumberItemAsync(5);
            }

            primeNumberItemsList = await App.PrimeNumberRepo.GetAllPrimeNumberItemsAsync().ConfigureAwait(false);

            foreach (var primeNumberItem in primeNumberItemsList)
            {
                primeNumberItemsObservableCollection.Add(primeNumberItem);
            }

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PrimeNumberListView.ItemsSource = primeNumberItemsObservableCollection;
            });
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // code here
            BiggestPrimeNumberTextBlock.Text = await App.PrimeNumberRepo.GetBiggestPrimeNumberAsStringAsync();
            var count = await App.PrimeNumberRepo.GetPrimeNumberItemsCountAsync();
            NumberOfPrimenumbersTextBlock.Text = count.ToString();
            // code here
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
        }

        private void SearchPrimeNumberTextChanged(object sender, TextChangedEventArgs e)
        {
            string trimmedPrimeNumber = SearchPrimeNumberTextBox.Text.Trim();
            if (int.TryParse(trimmedPrimeNumber, out int primeNumberCandidateAsInt))
            {
                PrimeNumberItem primeNumberItem = primeNumberItemsObservableCollection.FirstOrDefault(p => p.PrimeNumber >= primeNumberCandidateAsInt);

                if (primeNumberItem != null)
                {
                    if (primeNumberItem.PrimeNumber == int.Parse(trimmedPrimeNumber))
                    {
                        PrimeNumberListView.SelectedItem = primeNumberItem;
                    }
                    PrimeNumberListView.ScrollIntoView(primeNumberItem, ScrollIntoViewAlignment.Leading);
                }
            }
        }

        private async void ExportDataAppBarButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            StorageFile file = await HelpFileSavePicker.GetStorageFileForJsonAsync("SievePrimeNumbersNow");
            if (file != null)
            {
                ExportDataAppBarButton.IsEnabled = false;
                ExportDataProgressRing.IsEnabled = true;
                ExportDataProgressRing.IsActive = true;
                ExportDataProgressRing.Visibility = Visibility.Visible;

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    mainPage.NotifyUser("Exporting data. Please wait.", NotifyType.StatusMessage);
                });

                List<PrimeNumberItem> primeNumberItems = await App.PrimeNumberRepo.GetAllPrimeNumberItemsAsync().ConfigureAwait(false);

                string jsonData = await Task.Run(() => JsonConvert.SerializeObject(primeNumberItems)).ConfigureAwait(false);

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    // Prevent updates to the remote version of the file until we finish making changes and call CompleteUpdatesAsync.
                    CachedFileManager.DeferUpdates(file);
                    // write to file
                    await FileIO.WriteTextAsync(file, jsonData);
                    // Let Windows know that we're finished changing the file so the other app can update the remote version of the file.
                    // Completing updates may require Windows to ask for user input.
                    FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                    if (status == FileUpdateStatus.Complete)
                    {
                        mainPage.NotifyUser("File " + file.Name + " was saved.", NotifyType.StatusMessage);
                    }
                    else
                    {
                        mainPage.NotifyUser("File " + file.Name + " couldn't be saved.", NotifyType.StatusMessage);
                    }

                    ExportDataAppBarButton.IsEnabled = true;
                    ExportDataProgressRing.IsEnabled = false;
                    ExportDataProgressRing.IsActive = false;
                    ExportDataProgressRing.Visibility = Visibility.Collapsed;

                });
            }
            else
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    mainPage.NotifyUser("Operation cancelled.", NotifyType.StatusMessage);
                });
            }
        }

        private async void CompareDataAppBarButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            List<PrimeNumberItem> primeNumberItemsList = await App.PrimeNumberRepo.GetAllPrimeNumberItemsAsync();

            int lastKnownPrime = 0;
            bool equalPrimes = true;
            foreach (PrimeNumberItem primeNumberItem in primeNumberItemsList)
            {

                if (primeNumberItem.Id > SmallPrimes.smallPrimes.Length)
                {
                    break;
                }

                int knownPrime = SmallPrimes.smallPrimes[primeNumberItem.Id - 1];

                if (knownPrime != primeNumberItem.PrimeNumber)
                {
                    equalPrimes = false;
                    break;
                }

                lastKnownPrime = knownPrime;
            }

            if (equalPrimes)
            {
                mainPage.NotifyUser($"Primes are equal up to {lastKnownPrime}.", NotifyType.StatusMessage);
            }
            else
            {
                mainPage.NotifyUser($"Primes are not equal. Last good known prime is at {lastKnownPrime}.", NotifyType.ErrorMessage);
            }
        }
    }
}
