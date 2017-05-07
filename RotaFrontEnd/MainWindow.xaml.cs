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

            dataGrid.CellEditEnding += DataGrid_CellEditEnding;

            this.Closing += MainWindow_Closing;



        }

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            DataGridColumn col1 = e.Column;
            DataGridRow row1 = e.Row;
            int row_index = ((DataGrid)sender).ItemContainerGenerator.IndexFromContainer(row1);
            int col_index = col1.DisplayIndex;

            rota.RotaResults[row_index].SetUserFlag(col_index);

          
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
            SaveStaffRoster();
            rota.SerializeRota();
        }




        private void makeRota(object sender, RoutedEventArgs e)
        {
            if (startDatePicker.SelectedDate.HasValue && endDatePicker.SelectedDate.HasValue)
            {

                rota.GenerateRota();
                textBox1.Text = rota.GetRotaCountPrintOut();
                dataGrid.ItemsSource = rota.RotaResults;
            }
        }

        private void savePeopleButton(object sender, RoutedEventArgs e)
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

            staffDataGrid.ItemsSource = people.RotaPeople;


            reader.Close();


            rota.DeserializeRota(ci);

            dataGrid.ItemsSource = rota.RotaResults;

            if (rota.startTime.HasValue)
                startDatePicker.SelectedDate = rota.startTime;
            if (rota.endTime.HasValue)
                endDatePicker.SelectedDate = rota.endTime;
        }


    }
}
