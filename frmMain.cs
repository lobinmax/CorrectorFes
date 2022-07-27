using DevExpress.Utils.Animation;
using DevExpress.Utils.Extensions;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraTab.ViewInfo;

using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;

using Корректор_ФЭС.Properties;
using Корректор_ФЭС.XSD;

namespace Корректор_ФЭС
{
    public partial class frmMain : XtraForm
    {
        private string xmlFilePath;
        private string xmlFilePathOrigin;
        private СообщОперКО сообщОперКо;
        private PaymentSystemsBase CurrentPaymentSystem;
        private DataSet DsSourceXml;

        private DataRow GetPsSelectedRow => ((DataRowView)lookUpEditPaymentSystem.GetSelectedDataRow())?.Row;

        public frmMain()
        {
            InitializeComponent();

            var dtPS = new DataTable();
            dtPS.Columns.AddRange(new []
            {
                new DataColumn("Id", typeof(int)),
                new DataColumn("PaymentSystemName", typeof(string)),
                new DataColumn("HandlerClass", typeof(string)),
                new DataColumn("PathToFile", typeof(string))
            });

            dtPS.Rows.Add(0, "Золотая Корона", typeof(PsGoldenCrown).FullName, string.Empty);
            dtPS.Rows.Add(1, "Western Union", typeof(PsWesternUnion).FullName, string.Empty);
            dtPS.Rows.Add(2, "Юнистрим", typeof(PsUnistream).FullName, string.Empty);
            lookUpEditPaymentSystem.Properties.DataSource = dtPS;
        }

        private void frmMain_Resize(object sender, EventArgs e)
        {
            var formWidth = this.Size.Width - 8;
            var marginBySide = 12;
            var marginByPanel = 4;
            var panelSplit = 7;
            var panelWidth = (formWidth - (marginBySide * 2) - (marginByPanel * 2) - panelSplit) / 2;

            panelControl1.Size = new Size(panelWidth, panelControl1.Size.Height);
            panelControl2.Size = new Size(panelWidth, panelControl2.Size.Height);
            panelControl3.Size = new Size(panelWidth, panelControl3.Size.Height);
            panelControl4.Size = new Size(panelWidth, panelControl4.Size.Height);

            panelControl2.Location = new Point((marginBySide + panelWidth + marginByPanel + panelSplit + marginByPanel), panelControl2.Location.Y);
            panelControl4.Location = new Point((marginBySide + panelWidth + marginByPanel + panelSplit + marginByPanel), panelControl4.Location.Y);
            panelControl5.Location = new Point(marginBySide + panelWidth + marginByPanel, panelControl5.Location.Y);
        }

        private void BestFitColumns(GridView gridView)
        {
            gridView.BestFitColumns();
            gridView.Columns.ForEach(column =>
            {
                if (column.Width > 400)
                {
                    column.Width = 400;
                }
            });
        }
        
        private void gridControlXmlSource_ViewRegistered(object sender, ViewOperationEventArgs e)
        {
            if ((GridView)e.View == null) { return; }
            BestFitColumns((GridView)e.View);
        }

        private void CreateGridColumnCheckStatus(DataTable table)
        {
            if (gridViewXmlSource.Columns.Any(column => column.Name.Equals("gridColumnCheckStatus")))
            {
                gridViewXmlSource.Columns.Remove(gridViewXmlSource.Columns
                    .SingleOrDefault(column => column.Name.Equals("gridColumnCheckStatus", StringComparison.CurrentCultureIgnoreCase)));
            }
            if (table.Columns.Contains("CheckStatus"))
            {
                table.Columns.Remove("CheckStatus");
            }

            var gridColumnCheckStatus = new GridColumn();
            gridViewXmlSource.Columns.AddRange(new [] {gridColumnCheckStatus});

            gridColumnCheckStatus.AppearanceCell.Options.UseTextOptions = true;
            gridColumnCheckStatus.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            gridColumnCheckStatus.AppearanceCell.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            gridColumnCheckStatus.ColumnEdit = this.repositoryItemImageComboBox1;
            gridColumnCheckStatus.ImageOptions.Alignment = StringAlignment.Center;
            gridColumnCheckStatus.ImageOptions.Image = Resources.CheckBox_16x16;
            gridColumnCheckStatus.Name = "gridColumnCheckStatus";
            gridColumnCheckStatus.FieldName = "CheckStatus";
            gridColumnCheckStatus.Fixed = FixedStyle.Left;
            gridColumnCheckStatus.OptionsColumn.AllowEdit = false;
            gridColumnCheckStatus.OptionsColumn.AllowGroup = DevExpress.Utils.DefaultBoolean.False;
            gridColumnCheckStatus.OptionsColumn.AllowMove = false;
            gridColumnCheckStatus.OptionsColumn.AllowShowHide = false;
            gridColumnCheckStatus.OptionsColumn.AllowSize = false;
            gridColumnCheckStatus.OptionsColumn.FixedWidth = true;
            gridColumnCheckStatus.Visible = true;
            gridColumnCheckStatus.VisibleIndex = 0;
            gridColumnCheckStatus.Width = 35;

            table.Columns.Add("CheckStatus", typeof(string));
        }

        private void btnOpenXml_Click(object sender, EventArgs e)
        {
            var dlg = new XtraOpenFileDialog();
            dlg.CheckFileExists = true;
            dlg.Multiselect = false;
            dlg.CheckPathExists = true;
            dlg.Filter = @"Файл XML|*.xml";
            dlg.RestoreDirectory = true;
            dlg.FileName = xmlFilePath;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                xmlFilePath = Path.Combine(Program.GetTemporyCatalog, Path.GetFileName(dlg.FileName));
                xmlFilePathOrigin = dlg.FileName;
                File.Copy(dlg.FileName, xmlFilePath, true);
                LoadindDataFromXmlFile(xmlFilePath);
            }
        }

        private void btnVerifyDataPsWithDataXml_Click(object sender, EventArgs e)
        {
            if (сообщОперКо == null)
            {
                XtraMessageBox.Show("XML файл не загружен!", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (CurrentPaymentSystem == null)
            {
                XtraMessageBox.Show("Файл с данными платежной системы не загружен!", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            CurrentPaymentSystem.VerifyDataPsWithDataXml(сообщОперКо);

            UpdateOperationsAfterChecking();
        }

        private void xtraTabControl_CustomHeaderButtonClick(object sender, CustomHeaderButtonEventArgs e)
        {
            var btn = e.Button;
            if (btn.Index == 0)
            {
                if (XtraMessageBox.Show(
                    $"Данные будут обновлены из XML файла, изменения внесенные в операции сообщения будут утеряны.{Environment.NewLine}Продорлжить?",
                    Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes)
                {
                    LoadindDataFromXmlFile(xmlFilePathOrigin);
                }
            }
        }

        private bool ValidateXmlByXsd(string xmlPath)
        {
            var schemas = new XmlSchemaSet();
            var xsdPath = Program.GetXsd_SKO115FZ_OPER;
            var xsdPathTypes = Program.GetXsd_data_types;

            if (!Directory.Exists(Path.GetDirectoryName(xsdPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(xsdPath));
            }
            if (!File.Exists(xsdPath))
            {
                File.WriteAllBytes(xsdPath, Resources.SKO115FZ_OPER);
            }
            if (!File.Exists(xsdPathTypes))
            {
                File.WriteAllBytes(xsdPathTypes, Resources.data_types);
            }

            schemas.Add(null, xsdPath);
            schemas.Add(null, xsdPathTypes);

            var document = XDocument.Load(xmlPath);

            var validationEventArgsMessage = new List<string>();
            document.Validate(schemas, (sender_, validationEventArgs) =>
            {
                validationEventArgsMessage.Add(validationEventArgs.Message);
            });
            if (!validationEventArgsMessage.Any()) return true;

            XtraMessageBox.Show(
                $"Структура XML файла содержит следующие ошибки:{Environment.NewLine}{string.Join(Environment.NewLine, validationEventArgsMessage)}",
                Application.ProductName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return false;
        }

        private void UpdateWebBrowserUrl(string xmlPath)
        {
            if (!File.Exists(xmlPath))
            {
                XtraMessageBox.Show($"XML файл не найден!{Environment.NewLine}{xmlPath}",
                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            webBrowser1.Url = new Uri(xmlPath, UriKind.Absolute);
            webBrowser1.Update();

            xtraTabControl.TabPages.Remove(xtraTabControl.TabPages[1]);

            var pg = new DevExpress.XtraTab.XtraTabPage();
            xtraTabControl.TabPages.Add(pg);

            var webBr = new WebBrowser();
            webBr.AllowNavigation = false;
            webBr.AllowWebBrowserDrop = false;
            webBr.Dock = DockStyle.Fill;
            webBr.IsWebBrowserContextMenuEnabled = false;
            webBr.Location = new Point(0, 0);
            webBr.MinimumSize = new Size(20, 20);
            webBr.Name = "webBr";
            webBr.Size = new Size(634, 472);
            webBr.TabIndex = 0;
            webBr.WebBrowserShortcutsEnabled = false;

            pg.Controls.Add(webBr);
            pg.Name = "xtraTabPageXmlSourceXmlView";
            pg.Size = new Size(634, 472);
            pg.Text = @"Вид XML";

            webBr.Url = new Uri(xmlPath, UriKind.Absolute);
        }

        private async void LoadindDataFromXmlFile(string xmlPath)
        {
            using (new DisableControl(btnOpenXml))
            {
                await AmpereWaitAnimation.StartWaitingIndicator(
                    xtraTabControl,
                    WaitingAnimatorType.Line,
                    () =>
                    {
                        if (xtraTabControl.InvokeRequired)
                        {
                            this.Invoke(new Action(LocalMethod));
                        }
                        else
                        {
                            LocalMethod();
                        }
                    });
            }

            void LocalMethod()
            {
                if (!File.Exists(xmlPath))
                {
                    XtraMessageBox.Show($"XML файл не найден!{Environment.NewLine}{xmlPath}",
                        Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!ValidateXmlByXsd(xmlPath)) { return; }

                UpdateWebBrowserUrl(xmlPath);

                LoadingToDsSourceXml(xmlPath);

                BestFitColumns(gridViewXmlSource);

                CreateGridColumnCheckStatus(DsSourceXml.Tables["Операция"]);

                var xmlFileBytes = File.ReadAllBytes(xmlPath);
                using (var ms = new MemoryStream(xmlFileBytes))
                {
                    var xsr = new XmlSerializer(typeof(СообщОперКО));
                    сообщОперКо = (СообщОперКО)xsr.Deserialize(ms);
                    сообщОперКо.tableОперации = DsSourceXml.Tables["Операция"];
                }
            }
        }

        private void LoadingToDsSourceXml(string xmlPath)
        {
            if (!File.Exists(xmlPath))
            {
                XtraMessageBox.Show($"XML файл не найден!{Environment.NewLine}{xmlPath}",
                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var focusOperationNumberDr = gridViewXmlSource.GetFocusedDataRow();
            var focusOperationNumber = focusOperationNumberDr?["НомерОперация"].ToString();

            DsSourceXml = new DataSet();
            DsSourceXml.ReadXml(xmlPath);
            gridControlXmlSource.DataSource = DsSourceXml.Tables["Операция"];
            gridViewXmlSource.Columns[0].Summary.Clear();
            gridViewXmlSource.Columns[0].Summary.AddRange(
                new GridSummaryItem[]
                {
                    new GridColumnSummaryItem(DevExpress.Data.SummaryItemType.Count, "", "Всего: {0}")
                });
            gridViewXmlSource.FocusedRowHandle = gridViewXmlSource.LocateByValue("НомерОперация", focusOperationNumber);
        }

        private void lookUpEditPaymentSystem_ButtonClick(object sender, ButtonPressedEventArgs e)
        {
            var btn = e.Button;
            switch (btn.Index)
            {
                case 1: // Загрузить из папки
                    if (lookUpEditPaymentSystem.EditValue == null)
                    {
                        XtraMessageBox.Show($"Не выбрана платежная система!",
                            Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var dlg = new XtraOpenFileDialog();
                    dlg.CheckFileExists = true;
                    dlg.Multiselect = false;
                    dlg.CheckPathExists = true;
                    dlg.Filter = @"Файл Excel|*.xlsx";
                    dlg.RestoreDirectory = true;
                    dlg.FileName = GetPsSelectedRow["PathToFile"].ToString();
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        GetPsSelectedRow["PathToFile"] = dlg.FileName;

                        LoadPaymentSystemFile(dlg.FileName);
                    }

                    break;

                case 2: // обновить информацию
                    LoadPaymentSystemFile(GetPsSelectedRow["PathToFile"].ToString());

                    break;
            }
        }

        private async void LoadPaymentSystemFile(string pathToFile)
        {
            using (new DisableControl(lookUpEditPaymentSystem))
            {
                await AmpereWaitAnimation.StartWaitingIndicator(
                    gridControlPaySystemSourse, WaitingAnimatorType.Line,
                    () =>
                    {
                        if (gridControlPaySystemSourse.InvokeRequired)
                        {
                            this.Invoke(new Action(LocalMethod));
                        }
                        else
                        {
                            LocalMethod();
                        }
                    });
            }

            void LocalMethod()
            {
                if (string.IsNullOrEmpty(pathToFile) || !File.Exists(pathToFile))
                {
                    XtraMessageBox.Show($"Реестр с данным от платежной системы не найден!'" +
                                        $"{Environment.NewLine}Файл: '{pathToFile}'",
                        Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var type = Type.GetType($"{(string)GetPsSelectedRow["HandlerClass"]}");
                if (type == null)
                {
                    XtraMessageBox.Show($"Не найден класс - обработчик для платежной системы '{lookUpEditPaymentSystem.Text}'!" +
                                        $"{Environment.NewLine}Класс: {(string)lookUpEditPaymentSystem.EditValue}",
                                        Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                gridControlPaySystemSourse.DataSource = null;
                gridViewPaySystemSourse.PopulateColumns();

                CurrentPaymentSystem = (PaymentSystemsBase)Activator.CreateInstance(type, pathToFile);

                gridControlPaySystemSourse.DataSource = CurrentPaymentSystem.DataTablePaymentSystem;

                BestFitColumns(gridViewPaySystemSourse);
                gridViewPaySystemSourse.Columns[0].Summary.Clear();
                gridViewPaySystemSourse.Columns[0].Summary.AddRange(
                    new GridSummaryItem[] { new GridColumnSummaryItem(DevExpress.Data.SummaryItemType.Count, "", "Всего: {0}") });
            }
        }

        private void lookUpEditPaymentSystem_EditValueChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(GetPsSelectedRow["PathToFile"].ToString()))
            {
                LoadPaymentSystemFile(GetPsSelectedRow["PathToFile"].ToString());
            }
            else
            {
                gridControlPaySystemSourse.DataSource = null;
                gridViewPaySystemSourse.PopulateColumns();
            }
        }

        private void btnLoadManually_Click(object sender, EventArgs e)
        {
            if (сообщОперКо == null)
            {
                XtraMessageBox.Show("XML файл не загружен!", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (CurrentPaymentSystem == null)
            {
                XtraMessageBox.Show("Файл с данными платежной системы не загружен!", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var focusPsRow = gridViewPaySystemSourse.GetFocusedDataRow();
            if (focusPsRow == null)
            {
                XtraMessageBox.Show("Не выбрана строка с данными платежной системы!", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var focusXmlRow = gridViewXmlSource.GetFocusedDataRow();
            if (focusXmlRow == null)
            {
                XtraMessageBox.Show("Не выбрана строка с данными XML файла!", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            CurrentPaymentSystem.CreateSenderInXmlManually(сообщОперКо , focusXmlRow["НомерОперация"].ToString(), focusPsRow);
            UpdateOperationsAfterChecking();
        }

        private void btnAddOperation_Click(object sender, EventArgs e)
        {
            XtraMessageBox.Show("Функционал не реализован. Обратитесь к разработчику", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnEditOperation_Click(object sender, EventArgs e)
        {
            XtraMessageBox.Show("Функционал не реализован. Обратитесь к разработчику", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnDelOperation_Click(object sender, EventArgs e)
        {
            XtraMessageBox.Show("Функционал не реализован. Обратитесь к разработчику", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnSaveXmlFile_Click(object sender, EventArgs e)
        {
            if (сообщОперКо == null)
            {
                XtraMessageBox.Show("XML файл не загружен!", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!File.Exists(xmlFilePath))
            {
                XtraMessageBox.Show($"Исходный XML файл не найден, проверьте путь к файлу{Environment.NewLine}Путь: '{xmlFilePath}'", 
                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (DsSourceXml.Tables["Операция"].Rows
                .Cast<DataRow>().Any(row => string.IsNullOrEmpty(row["CheckStatus"].ToString())))
            {
                XtraMessageBox.Show($"В XML файл не загружалась информация из платежной системы",
                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!DsSourceXml.Tables["Операция"].Rows
                .Cast<DataRow>().All(row => row["CheckStatus"].ToString().Equals("0", StringComparison.CurrentCultureIgnoreCase)))
            {
                if (XtraMessageBox.Show(
                    $"В XML файл не по всем операциям была загружена информация из платежной системы.{Environment.NewLine}Продолжить сохранение?",
                    Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    return;
                }
            }

            var dlg = new XtraSaveFileDialog();
            dlg.Filter = @"Файл XML|*.xml";
            dlg.RestoreDirectory = true;
            dlg.AddExtension = true;
            dlg.FileName = $"{Path.GetFileName(xmlFilePath)}";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                File.Copy(xmlFilePath, dlg.FileName);
            }
        }

        private void UpdateOperationsAfterChecking()
        {
            сообщОперКо.ExportClassToXmlFile(xmlFilePath);

            var dtBefore = сообщОперКо.tableОперации.Rows.Cast<DataRow>();

            LoadingToDsSourceXml(xmlFilePath);

            BestFitColumns(gridViewXmlSource);

            CreateGridColumnCheckStatus(DsSourceXml.Tables["Операция"]);

            UpdateWebBrowserUrl(xmlFilePath);

            var dtAfter = DsSourceXml.Tables["Операция"];

            foreach (DataRow dtAfterRow in dtAfter.Rows)
            {
                dtAfterRow["CheckStatus"] = dtBefore
                    .SingleOrDefault(row => row["НомерОперация"].ToString()
                        .Equals(dtAfterRow["НомерОперация"].ToString(), StringComparison.CurrentCultureIgnoreCase))?["CheckStatus"];
            }
        }

    }
}