using System.Drawing;
using System.Windows.Forms;
using System;

namespace TrucXanhServer
{
    partial class ServerForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.groupBox1 = new GroupBox();
            this.pRank3 = new Label();
            this.pRank2 = new Label();
            this.pRank1 = new Label();
            this.startBtn = new Button();
            this.groupBox2 = new GroupBox();
            this.textBox1 = new TextBox();
            this.ipLabel = new Label();
            this.portLabel = new Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.pRank3);
            this.groupBox1.Controls.Add(this.pRank2);
            this.groupBox1.Controls.Add(this.pRank1);
            this.groupBox1.Location = new Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new Size(200, 132);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Danh sách người chơi";
            // 
            // pRank3
            // 
            this.pRank3.AutoSize = true;
            this.pRank3.Location = new Point(6, 107);
            this.pRank3.Name = "pRank3";
            this.pRank3.Size = new Size(35, 13);
            this.pRank3.TabIndex = 2;
            this.pRank3.Text = "label3";
            // 
            // pRank2
            // 
            this.pRank2.AutoSize = true;
            this.pRank2.Location = new Point(6, 69);
            this.pRank2.Name = "pRank2";
            this.pRank2.Size = new Size(35, 13);
            this.pRank2.TabIndex = 1;
            this.pRank2.Text = "label2";
            // 
            // pRank1
            // 
            this.pRank1.AutoSize = true;
            this.pRank1.Location = new Point(6, 33);
            this.pRank1.Name = "pRank1";
            this.pRank1.Size = new Size(35, 13);
            this.pRank1.TabIndex = 0;
            this.pRank1.Text = "label1";
            // 
            // startBtn
            // 
            this.startBtn.Cursor = Cursors.Hand;
            this.startBtn.FlatAppearance.BorderColor = Color.Red;
            this.startBtn.FlatAppearance.BorderSize = 12;
            this.startBtn.Font = new Font("Microsoft Sans Serif", 11F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.startBtn.Location = new Point(12, 319);
            this.startBtn.Name = "startBtn";
            this.startBtn.Size = new Size(200, 31);
            this.startBtn.TabIndex = 2;
            this.startBtn.Text = "Khởi động";
            this.startBtn.UseVisualStyleBackColor = true;
            this.startBtn.Click += new EventHandler(this.startBtn_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.textBox1);
            this.groupBox2.Location = new Point(230, 12);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new Size(408, 338);
            this.groupBox2.TabIndex = 3;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Console";
            // 
            // textBox1
            // 
            this.textBox1.HideSelection = false;
            this.textBox1.Location = new Point(6, 19);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.ScrollBars = ScrollBars.Vertical;
            this.textBox1.Size = new Size(396, 313);
            this.textBox1.TabIndex = 0;
            this.textBox1.Click += new EventHandler(this.textBox1_Click);
            // 
            // ipLabel
            // 
            this.ipLabel.AutoSize = true;
            this.ipLabel.Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(0)));
            this.ipLabel.Location = new Point(9, 180);
            this.ipLabel.Name = "ipLabel";
            this.ipLabel.Size = new Size(73, 15);
            this.ipLabel.TabIndex = 4;
            this.ipLabel.Text = "Server IP: ";
            // 
            // portLabel
            // 
            this.portLabel.AutoSize = true;
            this.portLabel.Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(0)));
            this.portLabel.Location = new Point(9, 204);
            this.portLabel.Name = "portLabel";
            this.portLabel.Size = new Size(41, 15);
            this.portLabel.TabIndex = 5;
            this.portLabel.Text = "Port: ";
            // 
            // ServerForm
            // 
            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(650, 362);
            this.Controls.Add(this.portLabel);
            this.Controls.Add(this.ipLabel);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.startBtn);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "ServerForm";
            this.Text = "Server - Trò chơi Trúc Xanh";
            this.Load += new EventHandler(this.ServerForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}