using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCP_chat.Common
{
    public class MyOpenFileDialog
    {
        public string OpenFileDialogMethod()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.InitialDirectory = "c:\\";
            openFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 2;
            openFileDialog.RestoreDirectory = true;

            string SendFileName = null;

            if (openFileDialog.ShowDialog() == true)
            {
                SendFileName = openFileDialog.FileName;
            }
            return SendFileName;
        }
    }
}