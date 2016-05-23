// --------------------------------------------------------------
// FilePathInput.cs is part of the VLAB project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-21-2016
// --------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Windows.Forms;
using System.IO;

namespace VLab
{
    public class FilePathInput : MonoBehaviour
    {
        public InputField input;

        public void OpenFile()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "Choose File";
            dialog.InitialDirectory = Directory.GetCurrentDirectory();
            dialog.Filter = "File (*.yaml;*.cs)|*.yaml;*.cs|All Files (*.*)|*.*";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                input.text = dialog.FileName;
                input.onEndEdit.Invoke(input.text);
            }
        }

        public void OpenDirectory()
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.ShowNewFolderButton = true;
            dialog.Description = "Choose Directory";
            if(dialog.ShowDialog() == DialogResult.OK)
            {
                input.text = dialog.SelectedPath;
                input.onEndEdit.Invoke(input.text);
            }
        }

    }
}