﻿using System;
using System.Collections.Generic;
using System.Text;
using LanExchange.Model;
using LanExchange.Model.VO;
using LanExchange;
using BrightIdeasSoftware;

namespace LanExchange.Model
{
    public class PersonProxy : PanelItemProxy
    {
        public new const string NAME = "PersonProxy";

        public PersonProxy()
            : base(NAME)
        {
        }

        public override OLVColumn[] GetColumns()
        {
            OLVColumn[] Result = new OLVColumn[] { 
                new OLVColumn(AppFacade.T("ColumnPersonName"), "Name"),
                new OLVColumn(AppFacade.T("ColumnPersonPosition"), "Ocupation"),
                new OLVColumn(AppFacade.T("ColumnPersonSubdiv"), "Subdiv")
            };
            Result[0].Width = 100;
            Result[1].Width = 100;
            Result[2].Width = 100;
            return Result;
        }

        public override void EnumObjects(string path)
        {
            Objects.Clear();
        }
    }
}
