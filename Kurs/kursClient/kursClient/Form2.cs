using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace kursClient
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
            this.FormClosing += Form2_FormClosing;
            this.checkBox1.CheckedChanged += CheckBox_CheckedChanged;
            this.checkBox2.CheckedChanged+= CheckBox_CheckedChanged;
            this.textBox1.Validated += TextBox1_Validated;
            this.textBox2.Validated += TextBox2_Validated;
            this.textBox3.Validated += TextBox3_Validated;
            //throw new Exception();
        }

        private void TextBox3_Validated(object sender, EventArgs e)
        {
            if (this.textBox3.Text.Trim() == "")
            {
                this.errorProvider1.SetError(this.textBox3, "Обязательно для заполнения");
            }
            else
            {
                if (this.textBox3.Text.Contains("/|!@#$%^&*()_+}{!<>?.,\"\\'"))
                {
                    this.errorProvider1.SetError(this.textBox3, "Поле не может содержать специальные символы \\/|\"\'");
                    // e.Cancel = true;
                }

                else
                    this.errorProvider1.Clear();
            }
        }

        private void TextBox2_Validated(object sender, EventArgs e)
        {
            if (this.textBox2.Text.Trim() == "")
            {
                this.errorProvider1.SetError(this.textBox2, "Обязательно для заполнения");
                //e.Cancel = true;
            }
            else
                 if (this.textBox2.Text.Contains("/|!@#$%^&*()_+}{!<>?.,\"\\'"))
            {
                this.errorProvider1.SetError(this.textBox2, "Поле не может содержать специальные символы \\/|\"\'");
                //e.Cancel = true;
            }
            else
                this.errorProvider1.Clear();
        }

        private void TextBox1_Validated(object sender, EventArgs e)
        {
            if (this.textBox1.Text.Trim() == "")
            {
                this.errorProvider1.SetError(this.textBox1, "Обязательно для заполнения");
                // e.Cancel = true;
            }
            else
            if (this.textBox1.Text.Contains("/|!@#$%^&*()_+}{!<>?.,\"\\'"))
            {
                this.errorProvider1.SetError(this.textBox1, "Поле может содержать только буквы и цифры");
                // e.Cancel = true;
            }
            else this.errorProvider1.Clear();
        }

        private void CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox check = (CheckBox)sender;
            if (check == this.checkBox1 && check.Checked)
            {
                checkBox2.Checked = false;
                return;
            }
            if (check == this.checkBox2 && check.Checked)
            {
                checkBox1.Checked = false;
                return;
            }

        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.errorProvider1.GetError(this.textBox1) !=""||
                this.errorProvider1.GetError(this.textBox2) != ""||
                this.errorProvider1.GetError(this.textBox3) != "")
                e.Cancel=true;
            //if (this.textBox1.Text.Trim() == "")
            //{
            //    this.errorProvider1.SetError(this.textBox1, "Обязательно для заполнения");
            //    e.Cancel = true;
            //}
            //else
            //if (this.textBox1.Text.Contains("/|!@#$%^&*()_+}{!<>?.,\"\\'"))
            //{
            //    this.errorProvider1.SetError(this.textBox1, "Поле может содержать только буквы и цифры");
            //    e.Cancel = true;
            //}
            //if (this.textBox2.Text.Trim() == "")
            //{
            //    this.errorProvider1.SetError(this.textBox2, "Обязательно для заполнения");
            //    e.Cancel = true;
            //}
            //else
            //if (this.textBox2.Text.Contains("/|!@#$%^&*()_+}{!<>?.,\"\\'"))
            //{
            //    this.errorProvider1.SetError(this.textBox2, "Поле не может содержать специальные символы \\/|\"\'");
            //    e.Cancel = true;
            //}

            //if (this.textBox3.Text.Trim() == "")
            //{
            //    this.errorProvider1.SetError(this.textBox3, "Обязательно для заполнения");
            //    e.Cancel = true;
            //}
            //  else 
            //{
            //    if (this.textBox3.Text.Contains("/|!@#$%^&*()_+}{!<>?.,\"\\'"))
            //    {
            //        this.errorProvider1.SetError(this.textBox3, "Поле не может содержать специальные символы \\/|\"\'");
            //        e.Cancel = true;
            //    }
            //    //Добавить проверку на емейл
            //    //        string expr =
            //    //"[.\\-_a-z0-9]+@([a-z0-9][\\-a-z0-9]+\\.)+[a-z]{2,6}";

            //    //        Match isMatch =
            //    //          Regex.Match(this.textBox3.Text, expr, RegexOptions.IgnoreCase);
            //    //if (!isMatch.Success)
            //    //{
            //    //    this.errorProvider1.SetError(this.textBox3, "Это не емейл");
            //    //    e.Cancel = true;
            //    //}
            //}

        }
    }
}
