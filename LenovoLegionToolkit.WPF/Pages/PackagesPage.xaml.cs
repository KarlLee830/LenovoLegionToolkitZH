﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Extensions;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.Controls.Packages;

namespace LenovoLegionToolkit.WPF.Pages
{
    public partial class PackagesPage : Page, IProgress<float>
    {
        private PackageDownloader _packageDownloader = IoCContainer.Resolve<PackageDownloader>();

        private CancellationTokenSource? _getPackagesTokenSource;

        private List<Package>? _packages;

        public PackagesPage()
        {
            InitializeComponent();
        }
        public void Report(float value) => Dispatcher.Invoke(() =>
        {
            _loader.IsIndeterminate = !(value > 0);
            _loader.Progress = value;
        });

        private async void DownloadPackagesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _downloadPackagesButton.Visibility = Visibility.Collapsed;
                _cancelDownloadPackagesButton.Visibility = Visibility.Visible;
                _loader.Visibility = Visibility.Visible;
                _loader.IsLoading = true;
                _packages = null;

                foreach (var control in _packagesStackPanel.Children.ToArray().OfType<PackageControl>())
                    control.CancelDownloads();

                _packagesStackPanel.Children.Clear();

                var machineType = _machineTypeTextBox.Text;
                var os = _osComboBox.SelectedIndex switch
                {
                    1 => "win10",
                    0 => "win11",
                    _ => null,
                };

                if (string.IsNullOrWhiteSpace(machineType) || machineType.Length != 4 || string.IsNullOrWhiteSpace(os))
                    return;

                _getPackagesTokenSource?.Cancel();
                _getPackagesTokenSource = new CancellationTokenSource();

                var token = _getPackagesTokenSource.Token;

                var packages = await _packageDownloader.GetPackagesAsync(machineType, os, this, token);

                _packages = packages;

                packages = Sort(packages);

                foreach (var package in packages)
                {
                    var control = new PackageControl(_packageDownloader, package);
                    _packagesStackPanel.Children.Add(control);
                }
            }
            catch (TaskCanceledException) { }
            finally
            {
                _downloadPackagesButton.Visibility = Visibility.Visible;
                _cancelDownloadPackagesButton.Visibility = Visibility.Collapsed;
                _loader.IsLoading = false;
                _loader.Progress = 0;
                _loader.IsIndeterminate = true;
            }
        }

        private void CancelDownloadPackagesButton_Click(object sender, RoutedEventArgs e) => _getPackagesTokenSource?.Cancel();

        private void SortingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_packages is null)
                return;

            foreach (var control in _packagesStackPanel.Children.ToArray().OfType<PackageControl>())
                control.CancelDownloads();

            _packagesStackPanel.Children.Clear();

            var packages = Sort(_packages);

            foreach (var package in packages)
            {
                var control = new PackageControl(_packageDownloader, package);
                _packagesStackPanel.Children.Add(control);
            }
        }

        private List<Package> Sort(List<Package> packages)
        {
            switch (_sortingComboBox.SelectedIndex)
            {
                case 0:
                    return packages.OrderBy(p => p.Description).ToList();
                case 1:
                    return packages.OrderBy(p => p.Category).ToList();
                case 2:
                    return packages.OrderByDescending(p => p.ReleaseDate).ToList();
                default:
                    return packages;
            }
        }
    }
}
