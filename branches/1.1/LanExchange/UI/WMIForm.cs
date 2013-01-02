﻿using System;
using System.Windows.Forms;
using LanExchange.Presenter;
using LanExchange.View;
using LanExchange.Model;
using System.Management;
using LanExchange.Utils;
using System.Collections.Generic;
using System.ComponentModel;

namespace LanExchange.UI
{
    public partial class WMIForm : Form, IWMIView
    {
        private readonly WMIPresenter m_Presenter;
        private string m_CurrentWMIClass;
        private readonly List<string> m_Classes;
        private ComputerPanelItem m_Comp;
        private ManagementObject wmiObject;

        public WMIForm(ComputerPanelItem comp)
        {
            InitializeComponent();
            m_Presenter = new WMIPresenter(comp, this);
            m_Classes = new List<string>();
            if (comp != null)
            {
                Text += String.Format(" — {0}", comp.Name);
                m_Comp = comp;
            }
        }

        public ListView LV
        {
            get { return lvInstances; }
        }

        public ContextMenuStrip MENU
        {
            get { return menuCommands; }
        }

        public string CurrentWMIClass
        {
            get
            {
                return m_CurrentWMIClass;
            }
            set
            {
                m_CurrentWMIClass = value;
                lDescription.Text = m_Presenter.GetClassDescription(value);
                lClassName.Text = value;
                m_Presenter.EnumObjects(value);
                if (lvInstances.Items.Count == 0)
                    PropGrid.SelectedObject = null;
                else
                {
                    lvInstances.FocusedItem = lvInstances.Items[0];
                    lvInstances.FocusedItem.Selected = true;
                    lvInstances_ItemActivate(lvInstances, new EventArgs());
                }
            }
        }

        private void WMIForm_Load(object sender, EventArgs e)
        {
            m_Presenter.EnumDynamicClasses();
            UpdateClassesMenu();
            CurrentWMIClass = "Win32_OperatingSystem";
        }

        public void ShowStat(int ClassCount, int PropCount, int MethodCount)
        {
            Status.Items[0].Text = String.Format("Классов: {0}, Свойств: {1}, Методов: {2}", ClassCount, PropCount, MethodCount);
        }

        public static void dynObj_AddProperty<T>(DynamicObject dynObj, PropertyData Prop, string Description, string Category, bool IsReadOnly)
        {
            if (Prop.Value == null)
                dynObj.AddPropertyNull<T>(Prop.Name, Description, Category, IsReadOnly);
            else
                if (Prop.IsArray)
                    dynObj.AddProperty<T[]>(Prop.Name, (T[])Prop.Value, Description, Category, IsReadOnly);
                else
                    dynObj.AddProperty<T>(Prop.Name, (T)Prop.Value, Description, Category, IsReadOnly);
        }

        private void lvInstances_ItemActivate(object sender, EventArgs e)
        {
            if (m_Presenter.WMIClass == null) return;
            if (lvInstances.FocusedItem == null) return;
            wmiObject = (ManagementObject)lvInstances.FocusedItem.Tag;
            if (wmiObject == null) return;

            var dynObj = new DynamicObject();
            foreach (PropertyData Prop in wmiObject.Properties)
            {
                // skip array of bytes
                if (Prop.Type == CimType.UInt8 && Prop.IsArray)
                    continue;
                
                PropertyData ClassProp = m_Presenter.WMIClass.Properties[Prop.Name];

                bool IsCIM_Key = false;
                bool IsReadOnly = true;
                string Description = "";

                foreach (QualifierData qd in ClassProp.Qualifiers)
                {
                    if (qd.Name.Equals("CIM_Key"))
                        IsCIM_Key = true;
                    if (qd.Name.Equals("write"))
                        IsReadOnly = false;
                    if (qd.Name.Equals("Description"))
                        Description = qd.Value.ToString();
                }
                if (IsCIM_Key) continue;
                string Category = Prop.Type.ToString();
                switch (Prop.Type)
                {

                    //     A signed 16-bit integer. This value maps to the System.Int16 type.
                    case CimType.SInt16:
                        dynObj_AddProperty<Int16>(dynObj, Prop, Description, Category, IsReadOnly);
                        break;
                    //     A signed 32-bit integer. This value maps to the System.Int32 type.
                    case CimType.SInt32:
                        dynObj_AddProperty<Int32>(dynObj, Prop, Description, Category, IsReadOnly);
                        break;
                    //     A floating-point 32-bit number. This value maps to the System.Single type.
                    case CimType.Real32:
                        dynObj_AddProperty<Single>(dynObj, Prop, Description, Category, IsReadOnly);
                        break;
                    //     A floating point 64-bit number. This value maps to the System.Double type.
                    case CimType.Real64:
                        dynObj_AddProperty<Double>(dynObj, Prop, Description, Category, IsReadOnly);
                        break;
                    //     A string. This value maps to the System.String type.
                    case CimType.String:
                        dynObj_AddProperty<String>(dynObj, Prop, Description, Category, IsReadOnly);
                        break;
                    //     A Boolean. This value maps to the System.Boolean type.
                    case CimType.Boolean:
                        dynObj_AddProperty<Boolean>(dynObj, Prop, Description, Category, IsReadOnly);
                        break;
                    //     An embedded object. Note that embedded objects differ from references in
                    //     that the embedded object does not have a path and its lifetime is identical
                    //     to the lifetime of the containing object. This value maps to the System.Object
                    //     type.
                    case CimType.Object:
                        dynObj_AddProperty<Object>(dynObj, Prop, Description, Category, IsReadOnly);
                        break;
                    //     A signed 8-bit integer. This value maps to the System.SByte type.
                    case CimType.SInt8:
                        dynObj_AddProperty<SByte>(dynObj, Prop, Description, Category, IsReadOnly);
                        break;
                    //     An unsigned 8-bit integer. This value maps to the System.Byte type.
                    case CimType.UInt8:
                        dynObj_AddProperty<Byte>(dynObj, Prop, Description, Category, IsReadOnly);
                        break;
                    //     An unsigned 16-bit integer. This value maps to the System.UInt16 type.
                    case CimType.UInt16:
                        dynObj_AddProperty<UInt16>(dynObj, Prop, Description, Category, IsReadOnly);
                        break;
                    //     An unsigned 32-bit integer. This value maps to the System.UInt32 type.
                    case CimType.UInt32:
                        dynObj_AddProperty<UInt32>(dynObj, Prop, Description, Category, IsReadOnly);
                        break;
                    //     A signed 64-bit integer. This value maps to the System.Int64 type.
                    case CimType.SInt64:
                        dynObj_AddProperty<Int64>(dynObj, Prop, Description, Category, IsReadOnly);
                        break;
                    //     An unsigned 64-bit integer. This value maps to the System.UInt64 type.
                    case CimType.UInt64:
                        dynObj_AddProperty<UInt64>(dynObj, Prop, Description, Category, IsReadOnly);
                        break;
                    //     A date or time value, represented in a string in DMTF date/time format: yyyymmddHHMMSS.mmmmmmsUUU,
                    //     where yyyymmdd is the date in year/month/day; HHMMSS is the time in hours/minutes/seconds;
                    //     mmmmmm is the number of microseconds in 6 digits; and sUUU is a sign (+ or
                    //     -) and a 3-digit UTC offset. This value maps to the System.DateTime type.
                    case CimType.DateTime:
                        if (Prop.Value == null)
                            dynObj.AddPropertyNull<DateTime>(Prop.Name, Description, Category, IsReadOnly);
                        else
                            dynObj.AddProperty<DateTime>(Prop.Name, WMIUtils.ToDateTime(Prop.Value.ToString()), Description, Category, IsReadOnly);
                        break;
                    //     A reference to another object. This is represented by a string containing
                    //     the path to the referenced object. This value maps to the System.Int16 type.
                    case CimType.Reference:
                        dynObj_AddProperty<Int16>(dynObj, Prop, Description, Category, IsReadOnly);
                        break;
                    //     A 16-bit character. This value maps to the System.Char type.
                    case CimType.Char16:
                        dynObj_AddProperty<Char>(dynObj, Prop, Description, Category, IsReadOnly);
                        break;
                    case CimType.None:
                    default:
                        string Value = Prop.Value == null ? null : Prop.Value.ToString();
                        dynObj.AddProperty<String>(String.Format("{0} : {1}", Prop.Name, Prop.Type), Value, Description, "Unknown", IsReadOnly);
                        break;
                }
            }
            PropGrid.SelectedObject = dynObj;
        }


        public void ClearClasses()
        {
            m_Classes.Clear();
        }

        public void AddClass(string ClassName)
        {
            m_Classes.Add(ClassName);
        }

        public void menuClasses_Click(object sender, EventArgs e)
        {
            CurrentWMIClass = (sender as ToolStripMenuItem).Text;
        }

        public void UpdateClassesMenu()
        {
            m_Classes.Sort();
            menuClasses.Items.Clear();
            m_Classes.ForEach(STR =>
            {
                ToolStripMenuItem MI = new ToolStripMenuItem { Text = STR };
                MI.Click += menuClasses_Click;
                menuClasses.Items.Add(MI);
            });
        }

        private void menuClasses_Opening(object sender, CancelEventArgs e)
        {
            foreach (ToolStripMenuItem MI in menuClasses.Items)
                MI.Checked = MI.Text.Equals(CurrentWMIClass);
        }

        private void PropGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            string PropName = e.ChangedItem.Label;
            object PropValue = e.ChangedItem.Value;
            string Caption = String.Format("Изменение свойства {0}", PropName);
            string Message = String.Format("Компьютер: \\\\{0}\n\nСтарое значение: «{1}»\nНовое значение: «{2}»",
                m_Comp.Name, e.OldValue, PropValue);
            try
            {
                // trying to change wmi property
                wmiObject[PropName] = PropValue;
                wmiObject.Put();

                // update computer comment if we changes Win32_OperatingSystme.Description
                if (CurrentWMIClass.Equals("Win32_OperatingSystem") && PropName.Equals("Description"))
                    m_Comp.Comment = PropValue.ToString();

                // property has been changed
                Message += String.Format("\n\nСвойство {0} успешно изменено.", PropName);
                MessageBox.Show(Message, Caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch(ManagementException ex)
            {
                // property not changed
                var dynObj = PropGrid.SelectedObject as DynamicObject;
                if (dynObj != null)
                    dynObj[PropName] = e.OldValue;
                Message += "\n\n" + ex.Message;
                MessageBox.Show(Message, Caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
}
