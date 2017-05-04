using RotaLib;
using RotaPrototype;
using System;
using System.Collections.Generic;
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

        public MainWindow()
        {
            InitializeComponent();

            rota = new SimpleFridayRota();

            LoadRotaPeople();

          

            comboBox.SelectedIndex = 0;

            comboBox.SelectionChanged += dateViewChanged;

            dateListBox.SelectionChanged += dateListBoxSelectionChanged;

            textBox1.IsReadOnly = true;

            CultureInfo ci = CultureInfo.CreateSpecificCulture(CultureInfo.CurrentCulture.Name);
            ci.DateTimeFormat.ShortDatePattern = "dd-MM-yyyy";
            Thread.CurrentThread.CurrentCulture = ci;


            staffDataGrid.CellEditEnding += StaffDataGrid_CellEditEnding;

            staffDataGrid.AddingNewItem += StaffDataGrid_AddingNewItem;

        }

        private void StaffDataGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            e.NewItem = new SimpleRotaPerson();
        }

        private void StaffDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            SaveStaffRoster();
        }

        private void dateListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            leavePicker.SelectedDate = dateListBox.SelectedItem as DateTime?;
        }

        private void dateViewChanged(object sender, SelectionChangedEventArgs e)
        {
            //var person = personListBox.SelectedItem as SimpleRotaPerson;
            //currentPersonSelection = person;
            //if (person != null)
            //{
            //    RefreshDateBox(person);
            //}
        }

       

        private void RefreshDateBox(SimpleRotaPerson person)
        {
            dateListBox.ItemsSource = null;
            dateListBox.Items.Clear();

            if (comboBox.SelectedIndex == 0)
                dateListBox.ItemsSource = person.AnnualLeaveDates;
            else
            {

                if (comboBox.SelectedIndex == 1)
                    PopulateListBoxWithSpecificRotaDates(currentPersonSelection, rota.OnCallRota);
                if (comboBox.SelectedIndex == 2)
                    PopulateListBoxWithSpecificRotaDates(currentPersonSelection, rota.OnCallRegistrarCoverRota);
                if (comboBox.SelectedIndex == 3)
                    PopulateListBoxWithSpecificRotaDates(currentPersonSelection, rota.SurgeryRota);
                if (comboBox.SelectedIndex == 4)
                    PopulateListBoxWithSpecificRotaDates(currentPersonSelection, rota.ProtectedRota);

            }
        }

        private void PopulateListBoxWithSpecificRotaDates(SimpleRotaPerson currentPersonSelection, Dictionary<RotaDate, SimpleRotaPerson> rota)
        {
            dateListBox.ItemsSource = null;
            dateListBox.Items.Clear();

            var knownOnCallDates = rota.Keys.ToList().FindAll(x => rota[x].Name == currentPersonSelection.Name);

            foreach (RotaDate d in knownOnCallDates)
                dateListBox.Items.Add(d.DateTime);
        }

       

       

     

        private void addDate(object sender, RoutedEventArgs e)
        {
            if (comboBox.SelectedIndex == 0)
            {

                if (currentPersonSelection != null)
                {
                    if (leavePicker.SelectedDate.HasValue)
                        currentPersonSelection.AddAnnualLeave(leavePicker.SelectedDate.Value);
                }
            }
            else
            {
                if (currentPersonSelection != null)
                {
                    if (leavePicker.SelectedDate.HasValue)
                    {
                        if (comboBox.SelectedIndex == 1)
                            rota.SetRotaForDate(rota.OnCallRota, new RotaDate(leavePicker.SelectedDate.Value), currentPersonSelection);
                        if (comboBox.SelectedIndex == 2)
                            rota.SetRotaForDate(rota.OnCallRegistrarCoverRota, new RotaDate(leavePicker.SelectedDate.Value), currentPersonSelection);
                        if (comboBox.SelectedIndex == 3)
                            rota.SetRotaForDate(rota.SurgeryRota, new RotaDate(leavePicker.SelectedDate.Value), currentPersonSelection);
                        if (comboBox.SelectedIndex == 4)
                            rota.SetRotaForDate(rota.ProtectedRota, new RotaDate(leavePicker.SelectedDate.Value), currentPersonSelection);


                    }

                    RefreshDateBox(currentPersonSelection);
                }
            }
        }

        private void removeDate(object sender, RoutedEventArgs e)
        {
            if (comboBox.SelectedIndex == 0)
            {
                if (currentPersonSelection != null)
                {
                    if (leavePicker.SelectedDate.HasValue)
                        if (currentPersonSelection.AnnualLeaveDates.Contains(leavePicker.SelectedDate.Value))
                            currentPersonSelection.AnnualLeaveDates.Remove(leavePicker.SelectedDate.Value);
                }
            }
            else
            {
                if (currentPersonSelection != null)
                {
                    if (leavePicker.SelectedDate.HasValue)
                    {
                        if (comboBox.SelectedIndex == 1)
                            rota.ClearRotaForDate(rota.OnCallRota, new RotaDate(leavePicker.SelectedDate.Value));
                        if (comboBox.SelectedIndex == 2)
                            rota.ClearRotaForDate(rota.OnCallRegistrarCoverRota, new RotaDate(leavePicker.SelectedDate.Value));
                        if (comboBox.SelectedIndex == 3)
                            rota.ClearRotaForDate(rota.SurgeryRota, new RotaDate(leavePicker.SelectedDate.Value));

                        if (comboBox.SelectedIndex == 4)
                            rota.ClearRotaForDate(rota.ProtectedRota, new RotaDate(leavePicker.SelectedDate.Value));




                    }

                    RefreshDateBox(currentPersonSelection);
                }
            }
        }

        private void makeRota(object sender, RoutedEventArgs e)
        {
            rota.MakeRota();
            textBox1.Text =  rota.GetRotaCountPrintOut();
            dataGrid.ItemsSource = rota.RotaResults;
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

        private void LoadRotaPeople()
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


            rota.DeserializeHistoricalRota();

        }

        private void saveHistoricalRota(object sender, RoutedEventArgs e)
        {
            rota.SerializeHistoricalRota();
        }
    }
}
