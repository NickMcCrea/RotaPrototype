using RotaLib;
using RotaPrototype;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;

namespace RotaFrontEnd
{



    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string personSaveFile = "rotaPeople.xml";
        SimpleFridayRota rota;
        SimpleRotaPerson currentPersonSelection;
        CultureInfo ci;
        DispatcherTimer t;
      

        public MainWindow()
        {
            InitializeComponent();

            ci = new CultureInfo("GB");
            ci.DateTimeFormat.ShortDatePattern = "dd-MM-yyyy";
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;

            rota = new SimpleFridayRota();

            LoadRota();


            startDatePicker.SelectedDateChanged += StartDatePicker_SelectedDateChanged;
            endDatePicker.SelectedDateChanged += EndDatePicker_SelectedDateChanged;

            textBox1.IsReadOnly = true;


            staffDataGrid.CanUserAddRows = true;
            staffDataGrid.CanUserDeleteRows = true;
            staffDataGrid.ItemsSource = rota.RotaPersons;

            dataGrid.CanUserAddRows = false;
            dataGrid.CanUserDeleteRows = false;
            dataGrid.ItemsSource = rota.RotaResults;
            dataGrid.CellEditEnding += DataGrid_CellEditEnding;


            t = new DispatcherTimer();
            t.Interval = new TimeSpan(0, 0, 0, 0, 500);
            t.Tick += T_Tick;
            this.Closing += MainWindow_Closing;


        }

        private void T_Tick(object sender, EventArgs e)
        {
            SetColours();
            t.Stop();
        }

        private void dtGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {

            SetColours();

        }
       
        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            DataGridColumn col1 = e.Column;
            DataGridRow row1 = e.Row;
            int row_index = ((DataGrid)sender).ItemContainerGenerator.IndexFromContainer(row1);
            int col_index = col1.DisplayIndex;

            if (e.EditingElement is TextBox)
            {

                if (string.IsNullOrEmpty(((TextBox)e.EditingElement).Text))
                {
                    rota.RotaResults[row_index].SetUserFlag(col_index, false);

                }
                else
                    rota.RotaResults[row_index].SetUserFlag(col_index, true);


                SetColours();

                button4.Background = Brushes.Red;
            }

        }

        private void EndDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            rota.endTime = endDatePicker.SelectedDate;
        }

        private void StartDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            rota.startTime = startDatePicker.SelectedDate;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

            if (startDatePicker.SelectedDate.Value > rota.RotaResults[0].rotaDate.DateTime)
            {

                MessageBoxResult result = MessageBox.Show("You have moved the start date - dates before the new start date will be purged from the rota. Confirm?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    SaveStaffRoster();
                    rota.SerializeRota();
                }
                else
                {
                    e.Cancel = true;
                }
            }
            else
            {
                SaveStaffRoster();
                rota.SerializeRota();
            }

            
        }

        private void SetColours()
        {
            int rowNum = 0;
            foreach (var row in rota.RotaResults)
            {
                var onCallCell = dataGrid.GetCell(rowNum, 1);
                SetCellColour(onCallCell, row.onCall.UserOverride, row.Locked);

                var coverCell = dataGrid.GetCell(rowNum, 2);
                SetCellColour(coverCell, row.cover.UserOverride, row.Locked);

                var surgeryCell = dataGrid.GetCell(rowNum, 3);
                SetCellColour(surgeryCell, row.surgery.UserOverride, row.Locked);

                var protectedCell = dataGrid.GetCell(rowNum, 4);
                SetCellColour(protectedCell, row.protectedTime.UserOverride, row.Locked);

                var leaveCell = dataGrid.GetCell(rowNum, 5);
                SetCellColour(leaveCell, row.onLeave.UserOverride, row.Locked);

                rowNum++;
            }

       


        }

        private void SetCellColour(DataGridCell cell, bool userOverride, bool rowLocked)
        {
            if (cell == null)
                return;

            if (userOverride)
            {
                cell.Background = Brushes.LightBlue;
              
            }
            else
            {
                if (rowLocked)
                    cell.Background = Brushes.Gray;
                else
                    cell.Background = Brushes.LightGray;
                
            }

        }

        private void MakeRota(object sender, RoutedEventArgs e)
        {

            if (startDatePicker.SelectedDate.HasValue && endDatePicker.SelectedDate.HasValue)
            {

                rota.GenerateRota();
                textBox1.Text = rota.GetRotaCountPrintOut();
                dataGrid.Items.Refresh();

            }

            t.Start();

            button4.Background = Brushes.LightGray;
        }

        private void SavePeopleButton(object sender, RoutedEventArgs e)
        {
            SaveStaffRoster();

        }

        private void SaveStaffRoster()
        {
            XmlSerializer xsSubmit = new XmlSerializer(typeof(RotaPersonCollection));
            var xml = "";


            RotaPersonCollection collection = new RotaPersonCollection();

            foreach (SimpleRotaPerson p in rota.RotaPersons)
                collection.RotaPeople = rota.RotaPersons.ToArray();


            using (var sww = new StringWriter())
            {
                using (XmlWriter writer = XmlWriter.Create(sww))
                {

                    xsSubmit.Serialize(writer, collection);

                }

                xml = sww.ToString(); // Your XML

            }

            File.WriteAllText(personSaveFile, xml);
        }

        private void LoadRota()
        {
            if (!File.Exists(personSaveFile))
                return;

            XmlSerializer serializer = new XmlSerializer(typeof(RotaPersonCollection));

            StreamReader reader = new StreamReader(personSaveFile);
            RotaPersonCollection people = serializer.Deserialize(reader) as RotaPersonCollection;

            foreach (SimpleRotaPerson person in people.RotaPeople)
                rota.RotaPersons.Add(person);

            staffDataGrid.ItemsSource = rota.RotaPersons;

            staffDataGrid.CanUserAddRows = true;
            staffDataGrid.CanUserDeleteRows = true;

            reader.Close();


            rota.DeserializeRota(ci);

            dataGrid.ItemsSource = rota.RotaResults;

            if (rota.startTime.HasValue)
                startDatePicker.SelectedDate = rota.startTime;
            if (rota.endTime.HasValue)
                endDatePicker.SelectedDate = rota.endTime;

            textBox1.Text = rota.GetRotaCountPrintOut();


        }



    }
}
