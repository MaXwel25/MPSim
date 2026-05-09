using System;
using System.IO;
using System.Windows;
using MPSim.Models;
using System.Windows.Controls;
using Microsoft.Win32;

namespace MPSim.UI
{
    public partial class ConfigWindow : Window
    {
        public SimulationConfig ResultConfig { get; private set; }

        public ConfigWindow(SimulationConfig initialConfig)
        {
            InitializeComponent();
            LoadConfig(initialConfig);
        }

        private void LoadConfig(SimulationConfig config)
        {
            txtPhases.Text = config.PhasesCount.ToString();
            txtJobs.Text = config.JobsCount.ToString();
            txtRuns.Text = config.NumRuns.ToString();
            txtLambda.Text = config.Lambda.ToString();
            txtMu.Text = config.Mu.ToString();
            txtSigma.Text = config.Sigma.ToString();
            txtSeed.Text = config.Seed.ToString();
            cbIntervalDist.SelectedIndex = config.IntervalDistribution;
            cbProcessingDist.SelectedIndex = config.ProcessingDistribution;
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ResultConfig = new SimulationConfig
                {
                    PhasesCount = int.Parse(txtPhases.Text),
                    JobsCount = int.Parse(txtJobs.Text),
                    NumRuns = int.Parse(txtRuns.Text),
                    Lambda = double.Parse(txtLambda.Text),
                    Mu = double.Parse(txtMu.Text),
                    Sigma = double.Parse(txtSigma.Text),
                    Seed = int.Parse(txtSeed.Text),
                    IntervalDistribution = cbIntervalDist.SelectedIndex,
                    ProcessingDistribution = cbProcessingDist.SelectedIndex
                };
                DialogResult = true;
                Close();
            }
            catch
            {
                MessageBox.Show("Неверный формат числовых данных.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}