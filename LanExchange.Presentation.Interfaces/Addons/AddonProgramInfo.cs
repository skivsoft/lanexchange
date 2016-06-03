﻿using System.Drawing;

namespace LanExchange.Presentation.Interfaces.Addons
{
    public sealed class AddonProgramInfo
    {
        public AddonProgramInfo(string expandedFileName, Image programImage)
        {
            ExpandedFileName = expandedFileName;
            ProgramImage = programImage;
        }

        public string ExpandedFileName { get; private set; }

        public Image ProgramImage { get; private set; }
    }
}