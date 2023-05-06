using IniParser;
using IniParser.Model;
using IniParser.Parser;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Shapes;

namespace Exposure_Profile_WPF_dotNetFramework
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public IniData iniData;
        public Dictionary<int, string[]> netDataDictionary;
        public struct GridData
        {
            public string Guid { get; set; }
            public string Name { get; set; }
            public string Profile { get; set; }
            public string UploadTime { get; set; }
            public string Holder { get; set; }
            public string SupportLayer { get; set; }
            public string Hash { get; set; }
            public string Status { get; set; }
        }
        public MainWindow()
        {
            InitializeComponent();
            SoftInformation();
        }
        private void SelectFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "ini文件|*.ini",
                Multiselect = false
            };
            if (openFileDialog.ShowDialog() == true)
            {
                string path = openFileDialog.FileName;
                FilePath.Text = path;
                if (ReadIniFile(path, out IniData iniData))
                {
                    this.iniData = iniData;
                    var section = iniData.Sections.GetSectionData("General");
                    SetGeneralInfo(section);
                }
            }
            else
            {
                FilePath.Text = null;
                RestAll();
            }

        }
        private void AddNew_Click(object sender, RoutedEventArgs e)
        {
            ItemCollection allItems = LayerThickness.Items;
            List<CheckBox> checkBoxes = new();
            if (NewItem.Text.Length > 0)
            {
                bool flag = true;
                foreach (CheckBox item in allItems)
                {
                    checkBoxes.Add(item);
                    if (item.Name.Equals("_" + NewItem.Text))
                    {
                        MessageBox.Show("该配置文件已存在！");
                        flag = false;
                    }
                }
                if (flag)
                {
                    CheckBox chk = new()
                    {
                        Name = "_" + NewItem.Text,
                        Content = NewItem.Text + "um",
                        Height = 15,
                        Width = 75
                    };
                    checkBoxes.Add(chk);
                    AddSections(NewItem.Text + "um");
                    LayerThickness.ItemsSource = checkBoxes;
                    LayerThickness_SourceUpdated();
                }
            }
        }
        private void DeleteSelect_Click(object sender, RoutedEventArgs e)
        {
            ItemCollection allItems = LayerThickness.Items;
            List<CheckBox> checkBoxes = new();
            foreach (CheckBox item in allItems)
            {
                if (item.IsChecked == false)
                {
                    checkBoxes.Add(item);
                }
                else
                {
                    DeleteSections(item.Content.ToString());
                }
            }
            LayerThickness.ItemsSource = checkBoxes;
            LayerThickness_SourceUpdated();
        }
        private void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(FilePath.Text))
            {
                FileIniDataParser parser = new();
                parser.WriteFile(FilePath.Text, iniData);
            }
            else if (FilePath.Text.Length == 0)
            {
                SaveAsNewFile_Click(sender, e);
            }
        }
        private void SaveAsNewFile_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new()
            {
                Filter = "ini|*.ini",

            };
            if (FilePath.Text.Length > 0 && File.Exists(FilePath.Text))
                saveFileDialog.FileName = new FileInfo(FilePath.Text).Name;
            if (saveFileDialog.ShowDialog() == true)
            {
                string newFileName = saveFileDialog.FileName;
                FileIniDataParser parser = new();
                parser.WriteFile(newFileName, iniData);
                FilePath.Text = newFileName;
            }
        }
        private void NewItemContentChanged(object sender, TextChangedEventArgs e)
        {
            TextBox temptbox = sender as TextBox;
            TextChange[] change = new TextChange[e.Changes.Count];
            e.Changes.CopyTo(change, 0);
            int offset = change[0].Offset;
            if (change[0].AddedLength > 0)
            {
                if (temptbox.Text.IndexOf(' ') != -1 || !int.TryParse(temptbox.Text, out _))
                {
                    temptbox.Text = temptbox.Text.Remove(offset, change[0].AddedLength);
                    temptbox.Select(offset, 0);
                }
                if (int.TryParse(temptbox.Text, out int num) && num > 1000)
                {
                    temptbox.Text = "1000";
                    temptbox.Select(4, 0);
                }
            }
        }
        private void SaveThisLayer_Click(object sender, RoutedEventArgs e)
        {
            var value = LayerThicknessComboBox.SelectedValue;
            if (value != null && !string.IsNullOrEmpty(value.ToString()))
            {
                string selectdSection = value.ToString();
                var section = iniData.Sections.GetSectionData(selectdSection);
                if (GetLayerInfo(section.SectionName, out SectionData sectionData))
                {
                    try
                    {
                        iniData.Sections.Add(sectionData);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "修改保存失败！");
                    }
                }

            }
        }
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var value = LayerThicknessComboBox.SelectedValue;
            if (value != null && !string.IsNullOrEmpty(value.ToString()))
            {
                string selectdSection = value.ToString();
                foreach (SectionData section in iniData.Sections)
                {
                    if (section.SectionName.Equals(selectdSection))
                    {
                        SetLayerInfo(section);
                    }
                }
            }
        }
        private void AdvancedExposure_Checked(object sender, RoutedEventArgs e)
        {
            //[]=>{}
            //exposureSequence="[
            //\"full\":[\"time_ms\":5000,\"power_mw\":20],
            //\"checkerboard_black\":[\"sides_px\":40,\"time_ms\":5000,\"power_mw\":20],
            //\"checkerboard_white\":[\"sides_px\":40,\"time_ms\":5000,\"power_mw\":20],
            //\"checkerboard_black_edge\":[\"sides_px\":40,\"edge_px\":3,\"time_ms\":5000,\"power_mw\":20],
            //\"checkerboard_white_edge\":[\"sides_px\":40,\"edge_px\":3,\"time_ms\":5000,\"power_mw\":20],
            //\"intersection\":[\"time_ms\":5000,\"power_mw\":20],
            //\"edge\":[\"edge_px\":3,\"time_ms\":5000,\"power_mw\":20],
            //\"black\":[\"time_ms\":5000]
            //]"
            if (AdvancedExposure.IsChecked == true)
            {

            }
        }
        private void Download_Click(object sender, RoutedEventArgs e)
        {
            GridData selectedProfile = (GridData)NetDataGrid.SelectedItem;
            SaveFileDialog saveFileDialog = new()
            {
                Filter = "ini|*.ini",
                FileName = selectedProfile.Name
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                var parser = new IniDataParser();
                iniData = parser.Parse(selectedProfile.Profile);
                new FileIniDataParser().WriteFile(saveFileDialog.FileName, iniData);
                FilePath.Text = saveFileDialog.FileName;
                foreach (SectionData section in iniData.Sections)
                {
                    if (section.SectionName.Equals("General"))
                    {
                        SetGeneralInfo(section);
                    }
                }
            }
        }
        private void Upload_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (iniData != null)
                {
                    if (NetDataGrid.ItemsSource == null)
                        LoadData();
                    DoUpload();
                }
                else
                {
                    MessageBox.Show("配置文档为空！");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void LoadNetData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadData();
            }
            catch (Exception ex)
            {
                NetDataGrid.Visibility = Visibility.Hidden;
                MessageBox.Show(ex.Message);
            }
            finally
            {
                LoadNetData.IsEnabled = true;
            }
        }
        private void LoadDataToLocal(object sender, RoutedEventArgs e)
        {
            if (NetDataGrid.SelectedItem != null)
            {
                GridData selectedProfile = (GridData)NetDataGrid.SelectedItem;
                var parser = new IniDataParser();
                iniData = parser.Parse(selectedProfile.Profile);
                foreach (SectionData section in iniData.Sections)
                {
                    if (section.SectionName.Equals("General"))
                    {
                        SetGeneralInfo(section);
                    }
                }
            }
        }
        private void DeleteNetData(object sender, RoutedEventArgs e)
        {
            if (NetDataGrid.SelectedItem != null)
            {
                GridData selectedProfile = (GridData)NetDataGrid.SelectedItem;
                if (PGsql.TaggingDeleteStatus(selectedProfile.Guid) && MessageBox.Show("注意！该操作将删除云端数据，该操作不可逆！", "是否删除云端配置文件？", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    MessageBox.Show("删除条目成功，列表将刷新！");
                    LoadData();
                }
            }
        }
        public static bool ReadIniFile(string path, out IniData iniData)
        {
            try
            {
                FileIniDataParser parser = new();
                iniData = parser.ReadFile(path);
                return IniReadCheck(iniData);
            }
            catch (Exception ex)
            {
                iniData = new IniData();
                MessageBox.Show(ex.Message);
            }
            return false;
        }
        public static bool IniReadCheck(IniData iniData)
        {
            string[] generalSectionNames = { "supportedLayerThickness", "wiperNeeded", "wavelengthPreferedInNM", "wavelengthToleranceInNM", "materialUsedForEachPixelInML" };
            //通过所有的段迭代
            foreach (SectionData section in iniData.Sections)
            {
                if (section.SectionName.Equals("General"))
                {
                    //遍历当前节中的所有键以打印值
                    foreach (KeyData key in section.Keys)
                    {
                        if (!generalSectionNames.Contains(key.KeyName))
                            return false;
                    }
                }
            }
            return true;
        }
        public void SetGeneralInfo(SectionData sectionData)
        {
            try
            {
                foreach (KeyData key in sectionData.Keys)
                {
                    if (key.KeyName.Equals("supportedLayerThickness"))
                    {
                        string[] tks = key.Value.Split(' ');
                        List<CheckBox> checkBoxes = new();

                        foreach (string thickness in tks)
                        {
                            if (thickness.Length > 0)
                            {
                                CheckBox chk = new()
                                {
                                    Name = "_" + thickness,
                                    Content = thickness,
                                    Height = 15,
                                    Width = 75
                                };
                                checkBoxes.Add(chk);
                            }
                        }
                        //IEnumerable<CheckBox> itemSource = checkBoxes;
                        LayerThickness.ItemsSource = checkBoxes;
                        LayerThickness_SourceUpdated();
                        //支持层厚
                    }
                    if (key.KeyName.Equals("wiperNeeded"))
                    {
                        //是否需要刮刀
                        WiperNeeded.IsChecked = !key.Value.Equals("false");
                    }
                    if (key.KeyName.Equals("wavelengthPreferedInNM"))
                    {
                        //中心波长
                        WaveLength.Text = key.Value + " nm";
                    }
                    if (key.KeyName.Equals("wavelengthToleranceInNM"))
                    {
                        //波长容差
                        Tolerance.Text = key.Value + " nm";
                    }
                    if (key.KeyName.Equals("materialUsedForEachPixelInML"))
                    {
                        MaterialUsed.Text = key.Value + " ml/pixel";
                        //材料消耗
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "载入文件出错！");
            }
        }
        public bool DeleteSections(string itemName)
        {
            try
            {
                IniData newIniData = new();
                SectionDataCollection sectionDatas = new();
                iniData.Sections.RemoveSection(itemName);
                /*
                foreach (SectionData section in iniData.Sections)
                {
                    if (!section.SectionName.Equals(itemName))
                    {
                        sectionDatas.Add(section);
                    }
                }
                */
                //newIniData.Sections = sectionDatas;
                //iniData = newIniData;
                UpdateGeneralSection_SupportLayer(itemName, "remove");
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }
        public bool AddSections(string itemName)
        {
            try
            {
                KeyData materialThicknessSpreadedInUM = new("materialThicknessSpreadedInUM")
                {
                    Value = (int.Parse(itemName.Split("um".ToCharArray())[0]) * 2).ToString(),
                };
                KeyData firstLayerThicknessInUM = new("firstLayerThicknessInUM")
                {
                    Value = itemName.Split("um".ToCharArray())[0]
                };
                KeyData firstLayerExposureTimeInS = new("firstLayerExposureTimeInS")
                {
                    Value = "1"
                };
                KeyData firstLayerExposurePowerInMWpCM2 = new("firstLayerExposurePowerInMWpCM2")
                {
                    Value = "20"
                };
                KeyData basementLayerExposureTimeInS = new("basementLayerExposureTimeInS")
                {
                    Value = "1"
                };
                KeyData basementLayerExposurePowerInMWpCM2 = new("basementLayerExposurePowerInMWpCM2")
                {
                    Value = "20"
                };
                KeyData normalLayerExposureTimeInS = new("normalLayerExposureTimeInS")
                {
                    Value = "1"
                };
                KeyData normalLayerExposurePowerInMWpCM2 = new("normalLayerExposurePowerInMWpCM2")
                {
                    Value = "20"
                };

                KeyDataCollection keyDatas = new();
                keyDatas.SetKeyData(materialThicknessSpreadedInUM);
                keyDatas.SetKeyData(firstLayerThicknessInUM);
                keyDatas.SetKeyData(firstLayerExposureTimeInS);
                keyDatas.SetKeyData(firstLayerExposurePowerInMWpCM2);
                keyDatas.SetKeyData(basementLayerExposureTimeInS);
                keyDatas.SetKeyData(basementLayerExposurePowerInMWpCM2);
                keyDatas.SetKeyData(normalLayerExposureTimeInS);
                keyDatas.SetKeyData(normalLayerExposurePowerInMWpCM2);
                SectionData sectionData = new(itemName)
                {
                    Keys = keyDatas
                };
                if (iniData == null)
                {
                    IniDataInitall(itemName);
                }
                iniData.Sections.Add(sectionData);
                UpdateGeneralSection_SupportLayer(itemName, "add");
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }
        public void IniDataInitall(string initallThickness)
        {
            KeyData supportedLayerThickness = new("supportedLayerThickness")
            {
                Value = initallThickness,
            };
            KeyData wiperNeeded = new("wiperNeeded")
            {
                Value = "true",
            };
            KeyData wavelengthPreferedInNM = new("wavelengthPreferedInNM")
            {
                Value = "405"
            };
            KeyData wavelengthToleranceInNM = new("wavelengthToleranceInNM")
            {
                Value = "20"
            };
            KeyData materialUsedForEachPixelInML = new("materialUsedForEachPixelInML")
            {
                Value = "1"
            };
            KeyDataCollection keyDatas = new();
            keyDatas.SetKeyData(supportedLayerThickness);
            keyDatas.SetKeyData(wiperNeeded);
            keyDatas.SetKeyData(wavelengthPreferedInNM);
            keyDatas.SetKeyData(wavelengthToleranceInNM);
            keyDatas.SetKeyData(materialUsedForEachPixelInML);
            SectionData sectionData = new("General")
            {
                Keys = keyDatas
            };
            iniData = new();
            iniData.Sections.Add(sectionData);
            SetGeneralInfo(sectionData);
        }
        public bool UpdateGeneralSection_SupportLayer(string updateSectionName, string updateType)
        {
            try
            {
                var section = iniData.Sections.GetSectionData("General");

                SectionData tmp = new("General");
                foreach (var keyvalues in section.Keys)
                {
                    if (keyvalues.KeyName.Equals("supportedLayerThickness"))
                    {
                        if (updateType.Equals("add") && !keyvalues.Value.Contains(updateSectionName))
                        {
                            keyvalues.Value = keyvalues.Value + " " + updateSectionName;
                        }
                        if (updateType.Equals("remove"))
                        {
                            keyvalues.Value = keyvalues.Value.Replace(updateSectionName, "");
                        }
                        tmp.Keys.SetKeyData(keyvalues);
                    }
                    else
                    {
                        tmp.Keys.SetKeyData(keyvalues);
                    }
                }
                iniData.Sections.RemoveSection("General");
                iniData.Sections.SetSectionData("General", tmp);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }
        public void SetLayerInfo(SectionData sectionData)
        {
            try
            {
                foreach (KeyData key in sectionData.Keys)
                {
                    if (key.KeyName.Equals("materialThicknessSpreadedInUM"))
                    {
                        MaterialThickness.Text = key.Value;
                    }
                    if (key.KeyName.Equals("firstLayerThicknessInUM"))
                    {
                        FLThickness.Text = key.Value;
                    }
                    if (key.KeyName.Equals("firstLayerExposureTimeInS"))
                    {
                        FLExposureTime.Text = key.Value;
                    }
                    if (key.KeyName.Equals("firstLayerExposurePowerInMWpCM2"))
                    {
                        FLExposurePower.Text = key.Value;
                    }
                    if (key.KeyName.Equals("basementLayerExposureTimeInS"))
                    {
                        BLExposureTime.Text = key.Value;
                    }
                    if (key.KeyName.Equals("basementLayerExposurePowerInMWpCM2"))
                    {
                        BLExposurePower.Text = key.Value;
                    }
                    if (key.KeyName.Equals("normalLayerExposureTimeInS"))
                    {
                        NLExposureTime.Text = key.Value;
                    }
                    if (key.KeyName.Equals("normalLayerExposurePowerInMWpCM2"))
                    {
                        NLExposurePower.Text = key.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "载入文件出错！");
            }
        }
        public bool GetLayerInfo(string sectionName, out SectionData sectionData)
        {

            sectionData = new SectionData(sectionName);
            try
            {
                KeyData materialThicknessSpreadedInUM = new("materialThicknessSpreadedInUM")
                {
                    Value = MaterialThickness.Text
                };
                KeyData firstLayerThicknessInUM = new("firstLayerThicknessInUM")
                {
                    Value = FLThickness.Text
                };
                KeyData firstLayerExposureTimeInS = new("firstLayerExposureTimeInS")
                {
                    Value = FLExposureTime.Text
                };
                KeyData firstLayerExposurePowerInMWpCM2 = new("firstLayerExposurePowerInMWpCM2")
                {
                    Value = FLExposurePower.Text
                };
                KeyData basementLayerExposureTimeInS = new("basementLayerExposureTimeInS")
                {
                    Value = BLExposureTime.Text
                };
                KeyData basementLayerExposurePowerInMWpCM2 = new("basementLayerExposurePowerInMWpCM2")
                {
                    Value = BLExposurePower.Text
                };
                KeyData normalLayerExposureTimeInS = new("normalLayerExposureTimeInS")
                {
                    Value = NLExposureTime.Text
                };
                KeyData normalLayerExposurePowerInMWpCM2 = new("normalLayerExposurePowerInMWpCM2")
                {
                    Value = NLExposurePower.Text
                };

                KeyDataCollection keyDatas = new();
                keyDatas.SetKeyData(materialThicknessSpreadedInUM);
                keyDatas.SetKeyData(firstLayerThicknessInUM);
                keyDatas.SetKeyData(firstLayerExposureTimeInS);
                keyDatas.SetKeyData(firstLayerExposurePowerInMWpCM2);
                keyDatas.SetKeyData(basementLayerExposureTimeInS);
                keyDatas.SetKeyData(basementLayerExposurePowerInMWpCM2);
                keyDatas.SetKeyData(normalLayerExposureTimeInS);
                keyDatas.SetKeyData(normalLayerExposurePowerInMWpCM2);
                sectionData.Keys = keyDatas;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "保存参数出错！");
                return false;
            }
        }
        public bool DicToTb(Dictionary<int, string[]> dataDictionary, out List<GridData> gridDatas)
        {
            gridDatas = new List<GridData>();
            try
            {
                foreach (var value in dataDictionary.Values)
                {
                    GridData gridData = new()
                    {
                        Guid = value[0],
                        Name = value[1],
                        Profile = value[2],
                        UploadTime = value[3].Replace(" ", "\n"),
                        Holder = value[4],
                        Hash = value[5],
                        Status = value[6]
                    };
                    string inidata = value[2];
                    var parser = new IniDataParser();
                    IniData ini = parser.Parse(inidata);
                    var section = ini.Sections.GetSectionData("General");
                    foreach (KeyData key in section.Keys)
                    {
                        if (key.KeyName.Equals("supportedLayerThickness"))
                        {
                            gridData.SupportLayer = key.Value;
                            break;
                        }
                    }
                    gridDatas.Add(gridData);
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }
        public static byte[] GetHash(string inputString)
        {
            HashAlgorithm algorithm = MD5.Create();  //or use SHA256.Create();
            return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }
        public static string GetHashString(string inputString)
        {
            StringBuilder sb = new();
            foreach (byte b in GetHash(inputString))
                sb.Append(b.ToString("X2"));
            return sb.ToString();
        }
        public void LayerThickness_SourceUpdated()
        {
            ItemCollection allItems = LayerThickness.Items;
            Layer.IsEnabled = allItems.Count > 0;
            if (Layer.IsEnabled)
            {
                List<string> strings = new();
                foreach (CheckBox item in allItems)
                {
                    strings.Add(item.Content.ToString());
                }
                IEnumerable<string> strings1 = strings;
                LayerThicknessComboBox.ItemsSource = strings1;
            }
        }
        public void RestAll()
        {
            LayerThickness.ItemsSource = null;
            WiperNeeded.IsChecked = false;
            LayerThickness_SourceUpdated();
        }
        private void SoftInformation()
        {
            try
            {
                Holder.Text = Environment.UserName;
                string version = ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString();
                StringBuilder info = new();
                info.Append("制作人：muyeyifeng\n");
                info.Append("版本：").Append(version).Append("\n");
                info.Append("联系方式：mfs@muyeyifeng.cn");
                Information.Content = info.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        public bool LoadData()
        {
            if (LoadNetData.Content.Equals("载入联网数据库"))
            {
                NetDataGrid.IsEnabled = true;
                LoadNetData.Content = "刷新";
            }
            else if (LoadNetData.Content.Equals("刷新"))
            {
                NetDataGrid.Visibility = Visibility.Hidden;
                NetDataGrid.ItemsSource = null;
            }
            if (PGsql.ReadDatas(out Dictionary<int, string[]> dataDictionary) && DicToTb(dataDictionary, out List<GridData> gridDatas))
            {
                netDataDictionary = dataDictionary;
                NetDataGrid.ItemsSource = gridDatas;
                NetDataGrid.Visibility = Visibility.Visible;
                return true;
            }
            return false;
        }
        public bool DoUpload()
        {
            Dictionary<string, string> data = new();
            FileInfo fileInfo = new(FilePath.Text);
            string hash = GetHashString(fileInfo.Name.Split('.')[0] + iniData.ToString());
            foreach (GridData item in NetDataGrid.ItemsSource)
            {
                if (item.Hash.Equals(hash))
                {
                    MessageBox.Show("已存在相同的配置文件");
                    return false;
                }
            }
            data.Add("name", fileInfo.Name.Split('.')[0]);
            data.Add("profile", iniData.ToString());
            data.Add("holder", Holder.Text);
            data.Add("hash", hash);
            StringBuilder warningMessage = new();
            warningMessage.Append("将以\"").Append(Holder.Text).Append("\"作为制作者上传该配置文档。\n");
            warningMessage.Append("该配置文档支持层厚:");
            var section = iniData.Sections.GetSectionData("General");
            foreach (KeyData key in section.Keys)
            {
                if (key.KeyName.Equals("supportedLayerThickness"))
                {
                    warningMessage.Append(key.Value);
                    break;
                }
            }


            warningMessage.Append("。");
            if (MessageBox.Show(warningMessage.ToString(), "是否上传配置文件？", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                if (PGsql.InsertSomeData(data))
                {
                    MessageBox.Show("上传成功，列表将刷新！");
                    LoadData();
                    return true;
                }
            }
            return false;
        }
        private void WiperNeeded_ChangeCheck(object sender, RoutedEventArgs e)
        {
            try
            {
                var section = iniData.Sections.GetSectionData("General");

                SectionData tmp = new("General");
                foreach (var keyvalues in section.Keys)
                {
                    if (keyvalues.KeyName.Equals("wiperNeeded"))
                    {
                        keyvalues.Value = WiperNeeded.IsChecked.Value ? "true" : "false";
                        tmp.Keys.SetKeyData(keyvalues);
                    }
                    else
                    {
                        tmp.Keys.SetKeyData(keyvalues);
                    }
                }
                iniData.Sections.RemoveSection("General");
                iniData.Sections.SetSectionData("General", tmp);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }
    }
}
