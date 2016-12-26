<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Form_info
    Inherits System.Windows.Forms.Form

    'Форма переопределяет dispose для очистки списка компонентов.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Является обязательной для конструктора форм Windows Forms
    Private components As System.ComponentModel.IContainer

    'Примечание: следующая процедура является обязательной для конструктора форм Windows Forms
    'Для ее изменения используйте конструктор форм Windows Form.  
    'Не изменяйте ее в редакторе исходного кода.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Form_info))
        Me.RichTextBox_info = New System.Windows.Forms.RichTextBox()
        Me.SuspendLayout()
        '
        'RichTextBox_info
        '
        Me.RichTextBox_info.Dock = System.Windows.Forms.DockStyle.Fill
        Me.RichTextBox_info.Location = New System.Drawing.Point(0, 0)
        Me.RichTextBox_info.Name = "RichTextBox_info"
        Me.RichTextBox_info.ReadOnly = True
        Me.RichTextBox_info.Size = New System.Drawing.Size(540, 351)
        Me.RichTextBox_info.TabIndex = 7
        Me.RichTextBox_info.Text = ""
        '
        'Form_info
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(540, 351)
        Me.Controls.Add(Me.RichTextBox_info)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "Form_info"
        Me.Text = "Для інформації"
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents RichTextBox_info As System.Windows.Forms.RichTextBox
End Class
