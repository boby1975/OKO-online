Imports System.IO
Imports System.ComponentModel

'server side
Public Class Form_main

    Private listener As System.Net.Sockets.TcpListener
    Private listenThread As System.Threading.Thread

    Private clients As New List(Of ConnectedClient) 'This list will store all connected clients.

    Const VER = "1.1 від 25.12.2016"

    Const MAX_OBJECT = 500 'maximum number of object
    Const MAX_OUTPUT = 8 'maximum number of outputs per object
    Const MAX_INPUT = 8 'maximum number of inputs per object
    Private object_row_height As Integer = 60

    Private view_220 As Integer = 1
    Private view_arm As Integer = 1
    Private view_time As Integer = 1
    Private save_log As Integer = 1
    Private msg_event As Integer = 1


    Const _220_ON = "220_on"
    Const _220_OFF = "220_off"
    Const LOW_ACC = "low_acc"
    Const ARMED = "armed"
    Const DISARMED = "disarmed"
    Const INPUT_NORMA = "input_norma_"
    Const INPUT_TREVOGA = "input_trevoga_"
    Const OUTPUT_ON = "output_on_"
    Const OUTPUT_OFF = "output_off_"
    Const COMMAND_PREF = "COMMAND:"
    Const COMMAND_END = ";"

    Private num_object As Integer = MAX_OBJECT
    Private num_input As Integer = MAX_INPUT
    Private num_output As Integer = MAX_OUTPUT
    Private max_num_object As Integer = MAX_OBJECT

    Private BtnArray_input(num_input, num_object) As Button
    Private BtnArray_output(num_output, num_object) As Button
    Private BtnArray_object(num_object) As Button
    Private BtnArray_arm(num_object) As Button
    Private BtnArray_time(num_object) As Label
    Private BtnArray_220(num_object) As Button

    Private startup_path As String

    Private EventQueue As New Queue() 'queue for events from objects

    Private LogQueue As New Queue() 'queue for log saving

    Private DataQueue_ As New Queue() 'queue for message receiving from object
    ' Creates a synchronized wrapper around the Queue.
    Private DataQueue As Queue = Queue.Synchronized(DataQueue_) 'each object in separate thread

    'Object properties
    Structure object_properties_str
        Dim name As String
        Dim notes As String
        Dim imei As String
        Dim code As String
        Dim arm As String
        Dim disarm As String
        Dim out1_on As String
        Dim out1_off As String
        Dim out2_on As String
        Dim out2_off As String
        Dim out3_on As String
        Dim out3_off As String
        Dim out4_on As String
        Dim out4_off As String
        Dim out5_on As String
        Dim out5_off As String
        Dim out6_on As String
        Dim out6_off As String
        Dim out7_on As String
        Dim out7_off As String
        Dim out8_on As String
        Dim out8_off As String
        Dim time_out_len As Integer
        Dim time_out As Integer
        Dim connected As Boolean
        Dim view As Boolean
    End Structure
    Private object_properties(0) As object_properties_str 'array of object's properties


    'Event properties
    Structure event_properties_str
        Dim event_code As String
        Dim audio_file As String
        Dim description As String
    End Structure
    Private event_properties(0) As event_properties_str 'array of activated events
    Private num_event As Integer = 0

    'message properties
    Structure mesage_properties_str
        Dim message As String
        Dim options As Integer
    End Structure
    Const REMOVE_CLIENT_GUI = 0
    Const APPEND_OUTPUT = 1
    Const APPEND_INFO = 2
    Const REFRESH_GUI = 3


    'for file
    Private f, fs, ts As Object
    Private f_r_name As String
    Const ForReading As Short = 1
    Const ForWriting As Short = 8
    Const ForAppending As Short = 3
    Const TristateUseDefault As Short = -2
    Const TristateTrue As Short = -1
    Const TristateFalse As Short = 0

    Private server_start As Boolean = False
    Private exit_program As Boolean = False

    Private bw_start_stop_button As BackgroundWorker = New BackgroundWorker

    Private bw_save_log As BackgroundWorker = New BackgroundWorker

    Private Sub Form1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        Try
            Dim i As Integer
            Dim k As Integer
            Dim offset As Integer = 2
            AddHandler bw_start_stop_button.DoWork, AddressOf bw_start_stop_button_DoWork
            AddHandler bw_start_stop_button.RunWorkerCompleted, AddressOf bw_start_stop_button_RunWorkerCompleted

            AddHandler bw_save_log.DoWork, AddressOf bw_save_log_DoWork
            AddHandler bw_save_log.RunWorkerCompleted, AddressOf bw_save_log_RunWorkerCompleted

            startup_path = Application.StartupPath()
            Me.Text = """OKO-online"" - станція моніторингу та керування приладів ОКО, версія " & VER


            open_config_file()
            open_object_file()
            open_event_file()


            AppendLog("СТАРТ ПРОГРАМИ")

            ReDim BtnArray_input(num_input, num_object)
            ReDim BtnArray_output(num_output, num_object)
            ReDim BtnArray_object(num_object)
            ReDim BtnArray_arm(num_object)
            ReDim BtnArray_time(num_object)
            ReDim BtnArray_220(num_object)


            For i = view_time + 1 + view_arm + view_220 + num_input + num_output To TableLayoutPanel_object.ColumnCount - 1
                TableLayoutPanel_object.ColumnStyles(i).SizeType = SizeType.AutoSize
            Next

            For i = 0 To view_time + 1 + view_arm + view_220 + num_input + num_output - 1
                TableLayoutPanel_object.ColumnStyles(i).SizeType = SizeType.Percent
                TableLayoutPanel_object.ColumnStyles(i).Width = 95 / (view_time + 1 + view_arm + view_220 + num_input + num_output)
            Next

            For i = 0 To num_object - 1
                TableLayoutPanel_object.RowStyles(TableLayoutPanel_object.RowCount - offset).Height = object_row_height
                TableLayoutPanel_object.RowStyles.Add(New RowStyle(SizeType.Absolute, object_row_height))
                TableLayoutPanel_object.RowCount += 1
            Next

            For i = 0 To num_object - 1

                If (view_time = 1) Then
                    BtnArray_time(i) = New Label()
                    BtnArray_time(i).Name = "time_" & CStr(i)
                    BtnArray_time(i).Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
                    BtnArray_time(i).TextAlign = ContentAlignment.MiddleCenter
                    'Set up the ToolTip text for the Button
                    ToolTip1.SetToolTip(BtnArray_time(i), "Час надходження останніх даних від об'єкту ") 
                End If

                BtnArray_object(i) = New Button()
                BtnArray_object(i).Name = "object_" & CStr(i)
                BtnArray_object(i).Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
                BtnArray_object(i).Text = object_properties(i).name
                BtnArray_object(i).BackColor = Color.Pink
                'Set up the ToolTip text for the Button
                ToolTip1.SetToolTip(BtnArray_object(i), object_properties(i).notes)
                TableLayoutPanel_object.Controls.Add(BtnArray_object(i), view_time, i) 'TableLayoutPanel_object.RowCount - offset
                AddHandler BtnArray_object(i).Click, AddressOf Object_ClickHandler


                If (view_arm = 1) Then
                    BtnArray_arm(i) = New Button()
                    BtnArray_arm(i).Name = "arm_" & CStr(i)
                    BtnArray_arm(i).Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
                    BtnArray_arm(i).Text = "arm"
                    BtnArray_arm(i).TextAlign = ContentAlignment.TopLeft '
                    'Set up the ToolTip text for the Button
                    ToolTip1.SetToolTip(BtnArray_arm(i), "Переключення стану охорони на протилежний - об'єкт " & BtnArray_object(i).Text)

                    BtnArray_arm(i).BackgroundImageLayout = ImageLayout.Stretch
                   
                    AddHandler BtnArray_arm(i).Click, AddressOf Arm_ClickHandler
                End If

                If (view_220 = 1) Then
                    BtnArray_220(i) = New Button()
                    BtnArray_220(i).Name = "220_" & CStr(i)
                    BtnArray_220(i).Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
                    BtnArray_220(i).Text = "220"
                    BtnArray_220(i).TextAlign = ContentAlignment.TopLeft ' 'ContentAlignment.MiddleCenter
                    'Set up the ToolTip text for the Button
                    ToolTip1.SetToolTip(BtnArray_220(i), "Поточний стан 220В на об'єкті " & BtnArray_object(i).Text)
                    BtnArray_220(i).BackgroundImageLayout = ImageLayout.Stretch
                    
                End If

                For k = 0 To num_input - 1
                    BtnArray_input(k, i) = New Button()
                    'Set up the ToolTip text for the Button
                    ToolTip1.SetToolTip(BtnArray_input(k, i), "Поточний стан входу-" & CStr(k + 1) & " на об'єкті " & BtnArray_object(i).Text)
                    BtnArray_input(k, i).Name = "input_" & CStr(i) & "_" & CStr(k + 1)
                    BtnArray_input(k, i).Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
                    BtnArray_input(k, i).Text = "i" & CStr(k + 1)
                    BtnArray_input(k, i).TextAlign = ContentAlignment.TopLeft
                    BtnArray_input(k, i).BackgroundImageLayout = ImageLayout.Stretch
                    
                Next


                For k = 0 To num_output - 1
                    BtnArray_output(k, i) = New Button()
                    'Set up the ToolTip text for the Button
                    ToolTip1.SetToolTip(BtnArray_output(k, i), "Переключити стан виходу-" & CStr(k + 1) & " на протилежний - об'єкт " & BtnArray_object(i).Text)
                    BtnArray_output(k, i).Name = "output_" & CStr(i) & "_" & CStr(k + 1)
                    BtnArray_output(k, i).Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
                    BtnArray_output(k, i).Text = "o" & CStr(k + 1)
                    BtnArray_output(k, i).TextAlign = ContentAlignment.TopLeft
                    BtnArray_output(k, i).BackgroundImageLayout = ImageLayout.Stretch
                    
                    AddHandler BtnArray_output(k, i).Click, AddressOf Output_ClickHandler
                Next

            Next
            Update_No_connectionGUI()

            Me.WindowState = FormWindowState.Maximized

        Catch ex As Exception
            MessageBox.Show(ex.ToString)
        End Try
    End Sub


    Private Sub StartStopButton_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles StartStopButton.CheckedChanged
        Try
            Me.Label_test.Text = "start/stop click "

            If Not bw_start_stop_button.IsBusy = True Then


                If StartStopButton.Checked Then
                    PortTextBox.Enabled = False
                    StartStopButton.Text = "Стоп"
                    StartStopButton.Image = My.Resources.Resources.stop_server
                    AppendLog("Натиснуто кнопку СТАРТ, порт: " & PortTextBox.Text)

                    listener = New System.Net.Sockets.TcpListener(System.Net.IPAddress.Any, CInt(PortTextBox.Text)) 'The TcpListener will listen for incoming connections at port 43001
                    listener.Start() 'Start listening.
                    listenThread = New System.Threading.Thread(AddressOf doListen) 'This thread will run the doListen method
                    listenThread.IsBackground = True 'Since we dont want this thread to keep on running after the application closes, we set isBackground to true.
                    listenThread.Start() 'Start executing doListen on the worker thread.
                    server_start = True

                Else
                    PortTextBox.Enabled = True
                    StartStopButton.Text = "Старт"
                    StartStopButton.Image = My.Resources.Resources.start_server
                    AppendLog("Натиснуто кнопку СТОП.")

                    bw_start_stop_button.RunWorkerAsync()



                End If
            End If
        Catch ex As Exception
            MessageBox.Show(ex.ToString)
        End Try
    End Sub

    Private Sub bw_start_stop_button_DoWork(ByVal sender As Object, ByVal e As DoWorkEventArgs)
        Try
            'close active sockets
            For Each opened_socket As ConnectedClient In clients
                opened_socket.CloseSocket()
            Next
            'clients.Clear()
            listener.Stop()
            listener = Nothing
            listenThread = Nothing
            server_start = False

        Catch ex As Exception
            MessageBox.Show(ex.ToString)
        End Try


    End Sub

    Private Sub bw_start_stop_button_RunWorkerCompleted(ByVal sender As Object, ByVal e As RunWorkerCompletedEventArgs)
        Try
            AppendLog("Виконано СТОП.")
            Me.Label_test.Text = "start/stop done"
        Catch ex As Exception
            MessageBox.Show(ex.ToString)
        End Try
    End Sub
    Public Sub Arm_ClickHandler(ByVal sender As Object, ByVal e As  _
   System.EventArgs)
        Try
            Me.Label_test.Text = "arm click "
            Dim client As ConnectedClient
            Dim fields As String() = Strings.Split(CType(sender, Button).Name, "_")
            If fields.Length = 2 Then
                If (object_properties(CInt(fields(1))).connected) Then
                    client = GetClientByIMEI(object_properties(CInt(fields(1))).imei)
                    If Not (client Is Nothing) Then
                        If (BtnArray_arm(CInt(fields(1))).Tag) Then
                            client.SendMessage(COMMAND_PREF & object_properties(CInt(fields(1))).code & object_properties(CInt(fields(1))).disarm & COMMAND_END)
                            AppendLog("Натиснуто кнопку: зняття з охорони об'єкту " & object_properties(CInt(fields(1))).name & " (IMEI=" & object_properties(CInt(fields(1))).imei & ")")
                        Else
                            client.SendMessage(COMMAND_PREF & object_properties(CInt(fields(1))).code & object_properties(CInt(fields(1))).arm & COMMAND_END)
                            AppendLog("Натиснуто кнопку: постановка на охорону об'єкту " & object_properties(CInt(fields(1))).name & " (IMEI=" & object_properties(CInt(fields(1))).imei & ")")
                        End If
                    End If
                Else
                    No_Connection_Msg(object_properties(CInt(fields(1))).name)
                End If
            End If
        Catch ex As Exception
            MessageBox.Show(ex.ToString)
        End Try
    End Sub


    Public Sub Object_ClickHandler(ByVal sender As Object, ByVal e As  _
   System.EventArgs)
        Try
            Me.Label_test.Text = "object click "

            Dim client As ConnectedClient

            Dim fields As String() = Strings.Split(CType(sender, Button).Name, "_")
            If fields.Length = 2 Then
                If (object_properties(CInt(fields(1))).connected) Then
                    client = GetClientByIMEI(object_properties(CInt(fields(1))).imei)
                    If Not (client Is Nothing) Then
                        client.SendMessage(COMMAND_PREF & object_properties(CInt(fields(1))).code & "70" & COMMAND_END)
                        AppendLog("Натиснуто кнопку: запит стану об'єкту " & object_properties(CInt(fields(1))).name & " (IMEI=" & object_properties(CInt(fields(1))).imei & ")")
                    End If
                Else
                    No_Connection_Msg(object_properties(CInt(fields(1))).name)
                End If
            End If
        Catch ex As Exception
            MessageBox.Show(ex.ToString)
        End Try
    End Sub


    Public Sub Output_ClickHandler(ByVal sender As Object, ByVal e As  _
   System.EventArgs)
        Try
            Me.Label_test.Text = "output click "
           
            Dim client As ConnectedClient
            Dim fields As String() = Strings.Split(CType(sender, Button).Name, "_")
            If fields.Length = 3 Then
                If (object_properties(CInt(fields(1))).connected) Then
                    client = GetClientByIMEI(object_properties(CInt(fields(1))).imei)
                    If Not (client Is Nothing) Then
                        If (BtnArray_output(CInt(fields(2)) - 1, CInt(fields(1))).Tag) Then
                            If (CInt(fields(2)) = 1) Then client.SendMessage(COMMAND_PREF & object_properties(CInt(fields(1))).code & object_properties(CInt(fields(1))).out1_off & COMMAND_END)
                            If (CInt(fields(2)) = 2) Then client.SendMessage(COMMAND_PREF & object_properties(CInt(fields(1))).code & object_properties(CInt(fields(1))).out2_off & COMMAND_END)
                            If (CInt(fields(2)) = 3) Then client.SendMessage(COMMAND_PREF & object_properties(CInt(fields(1))).code & object_properties(CInt(fields(1))).out3_off & COMMAND_END)
                            If (CInt(fields(2)) = 4) Then client.SendMessage(COMMAND_PREF & object_properties(CInt(fields(1))).code & object_properties(CInt(fields(1))).out4_off & COMMAND_END)
                            If (CInt(fields(2)) = 5) Then client.SendMessage(COMMAND_PREF & object_properties(CInt(fields(1))).code & object_properties(CInt(fields(1))).out5_off & COMMAND_END)
                            If (CInt(fields(2)) = 6) Then client.SendMessage(COMMAND_PREF & object_properties(CInt(fields(1))).code & object_properties(CInt(fields(1))).out6_off & COMMAND_END)
                            If (CInt(fields(2)) = 7) Then client.SendMessage(COMMAND_PREF & object_properties(CInt(fields(1))).code & object_properties(CInt(fields(1))).out7_off & COMMAND_END)
                            If (CInt(fields(2)) = 8) Then client.SendMessage(COMMAND_PREF & object_properties(CInt(fields(1))).code & object_properties(CInt(fields(1))).out8_off & COMMAND_END)
                            AppendLog("Натиснуто кнопку: вимкнути вихід-" & CInt(fields(2)) & " на об'єкті " & object_properties(CInt(fields(1))).name & " (IMEI=" & object_properties(CInt(fields(1))).imei & ")")
                        Else
                            If (CInt(fields(2)) = 1) Then client.SendMessage(COMMAND_PREF & object_properties(CInt(fields(1))).code & object_properties(CInt(fields(1))).out1_on & COMMAND_END)
                            If (CInt(fields(2)) = 2) Then client.SendMessage(COMMAND_PREF & object_properties(CInt(fields(1))).code & object_properties(CInt(fields(1))).out2_on & COMMAND_END)
                            If (CInt(fields(2)) = 3) Then client.SendMessage(COMMAND_PREF & object_properties(CInt(fields(1))).code & object_properties(CInt(fields(1))).out3_on & COMMAND_END)
                            If (CInt(fields(2)) = 4) Then client.SendMessage(COMMAND_PREF & object_properties(CInt(fields(1))).code & object_properties(CInt(fields(1))).out4_on & COMMAND_END)
                            If (CInt(fields(2)) = 5) Then client.SendMessage(COMMAND_PREF & object_properties(CInt(fields(1))).code & object_properties(CInt(fields(1))).out5_on & COMMAND_END)
                            If (CInt(fields(2)) = 6) Then client.SendMessage(COMMAND_PREF & object_properties(CInt(fields(1))).code & object_properties(CInt(fields(1))).out6_on & COMMAND_END)
                            If (CInt(fields(2)) = 7) Then client.SendMessage(COMMAND_PREF & object_properties(CInt(fields(1))).code & object_properties(CInt(fields(1))).out7_on & COMMAND_END)
                            If (CInt(fields(2)) = 8) Then client.SendMessage(COMMAND_PREF & object_properties(CInt(fields(1))).code & object_properties(CInt(fields(1))).out8_on & COMMAND_END)
                            AppendLog("Натиснуто кнопку: ввімкнути вихід-" & CInt(fields(2)) & " на об'єкті " & object_properties(CInt(fields(1))).name & " (IMEI=" & object_properties(CInt(fields(1))).imei & ")")
                        End If
                    End If
                Else
                    No_Connection_Msg(object_properties(CInt(fields(1))).name)
                End If
            End If


        Catch ex As Exception
            MessageBox.Show(ex.ToString)
        End Try
    End Sub


    Public Sub No_Connection_Msg(ByVal object_ As String)
        Try
            Try
                My.Computer.Audio.Play(startup_path & "\config\fault.wav")
            Catch ex As Exception
            End Try
            MsgBox("Відсутній зв'язок з об'єктом " & object_)
        Catch ex As Exception
            MessageBox.Show(ex.ToString)
        End Try
        
    End Sub



    Private Sub doListen()
        Try
            Dim incomingClient As System.Net.Sockets.TcpClient
           
            Do

                incomingClient = listener.AcceptTcpClient 'Accept the incoming connection. This is a blocking method so execution will halt here until someone tries to connect.

                Dim connClient As New ConnectedClient(incomingClient, Me) 'Create a new instance of ConnectedClient (check its constructor to see whats happening now).

                AddHandler connClient.dataReceived, AddressOf Me.messageReceived

                clients.Add(connClient) 'Adds the connected client to the list of connected clients.

            Loop

        Catch ex As Exception

            'MessageBox.Show(ex.ToString)

        End Try
    End Sub



    Public Sub RemoveClient(ByVal client As ConnectedClient)
        Try

            If clients.Contains(client) Then

                clients.Remove(client)

            End If

        Catch ex As Exception

            MessageBox.Show(ex.ToString)

        End Try
    End Sub





    Private Sub AppendLog(message As String)
        Try
            message = Now().ToString("dd/MM/yy H:mm:ss") & "-->" & message & ControlChars.NewLine

            If (save_log = 1) Then LogQueue.Enqueue(message)

            If Form_log.RichTextBox_log.Text.Length <= 40000 Then

            Else
                Form_log.RichTextBox_log.Text = Mid(Form_log.RichTextBox_log.Text, 20000)
            End If

            Form_log.RichTextBox_log.AppendText(message)
            Form_log.RichTextBox_log.ScrollToCaret()

        Catch ex As Exception
            MessageBox.Show(ex.ToString)
        End Try
    End Sub



    Private Sub AppendInfo(message As String)
        Try
            AppendLog(message)

            If Form_info.RichTextBox_info.Text.Length <= 40000 Then

            Else
                Form_info.RichTextBox_info.Text = Mid(Form_info.RichTextBox_info.Text, 20000)
            End If

            message = Now().ToString("dd/MM/yy H:mm:ss") & "-->" & message & ControlChars.NewLine
            Form_info.RichTextBox_info.AppendText(message)
            Form_info.RichTextBox_info.ScrollToCaret()
            Form_info.Show()
        Catch ex As Exception
            MessageBox.Show(ex.ToString)
        End Try
    End Sub


    Private Sub Update_No_connectionGUI()
        Try
            Dim i As Integer
            Dim row_num As Integer = 0
            DataGridView_no_connection.Rows.Clear()
            DataGridView_no_connection.Columns.Clear()
            DataGridView_no_connection.Columns.Add("name", "Об'єкт")
            DataGridView_no_connection.Columns.Add("notes", "Опис")
            For i = 0 To num_object - 1
                If (Not object_properties(i).connected) Then
                    DataGridView_no_connection.Rows.Add(1)
                    row_num += 1
                    DataGridView_no_connection(0, row_num - 1).Value = object_properties(i).name
                    DataGridView_no_connection(0, row_num - 1).Style.BackColor = Color.Pink
                    DataGridView_no_connection(1, row_num - 1).Value = object_properties(i).notes
                End If
            Next
            'Me.DataGridView_report.RowHeadersWidth = 30
            'Me.DataGridView_report.Columns(0).Width = 180
        Catch ex As Exception
            MessageBox.Show(ex.ToString)
        End Try
    End Sub


    Private Function hex_8bits(s As String) As String
        Dim state As String = ""
        Try
            Dim i As Integer = Convert.ToInt32(s, 16) 'conver hex to integer
            state = Convert.ToString(i, 2) 'convert integer to binary string
            If (state.Length = 1) Then state = "0000000" & state
            If (state.Length = 2) Then state = "000000" & state
            If (state.Length = 3) Then state = "00000" & state
            If (state.Length = 4) Then state = "0000" & state
            If (state.Length = 5) Then state = "000" & state
            If (state.Length = 6) Then state = "00" & state
            If (state.Length = 7) Then state = "0" & state

        Catch ex As Exception
            MessageBox.Show(ex.ToString)
        End Try
        Return state
    End Function


    Private Sub RefreshGUI(data As String)
        Try
            Dim i, k As Integer
            Dim fields As String() = Strings.Split(data, ",")
            Const OKO_S2 As Integer = 1  '{866104020810332,F9,0B,05C8,1F,3.0.3,OKO-S2}
            Const OKO_U2_PRO As Integer = 2  '{863591021960863,00F9,5600,2F202000,0000,12,1A80808080808080,0000CCF10000000080CA1A7D0000B9DB00000000000000000000000000000000,072B06DE0000000000000FFB0FF90DEB0FFE0FFE0FFE0FFE0FFE0FFE0FFE0FFE,1.2.3,OKO-PRO,,,,,,,,,000007,}
            Const OKO_U As Integer = 3 '{UD123456789098765,FB,С0,02,7А,5C,27,-10,-5,25,40,32,3,19,128,15grn,3.9}
            Dim protocol_type As Integer = 0


            For i = 0 To num_object - 1
                If (object_properties(i).imei.Equals(fields(0))) Then
                    'analize protocol
                    If (fields.Length > 6) Then
                        If (fields(6).Equals("OKO-S2")) Then
                            protocol_type = OKO_S2
                        Else
                            If (fields.Length > 10) Then
                                If (fields(10).Equals("OKO-PRO") Or fields(10).Equals("OKO-U2")) Then
                                    protocol_type = OKO_U2_PRO
                                Else
                                    If (fields.Length > 16) Then
                                        If (fields(0).Substring(0, 2).Equals("UD")) Then protocol_type = OKO_U
                                    End If
                                End If

                            End If
                        End If
                    End If

                    If (protocol_type = OKO_S2 Or protocol_type = OKO_U2_PRO Or protocol_type = OKO_U) Then
                        Dim est220 As Boolean = False
                        Dim lowbat As Boolean = False
                        Dim ohrana As Boolean = False
                        Dim input(8) As Boolean
                        Dim output(8) As Boolean
                        Dim state As String = ""
                        Dim event_code As Integer = 200

                        'set timeout for data receiving
                        'Dim thisLock As New Object
                        'SyncLock thisLock
                        object_properties(i).time_out = object_properties(i).time_out_len
                        'End SyncLock

                        'event analizing
                        For k = 0 To num_event - 1

                            If (protocol_type = OKO_U) Then
                                event_code = Convert.ToInt32(fields(1), 16) 'conver hex to integer
                            Else
                                event_code = 300 + Convert.ToInt32(fields(1), 16) 'conver hex to integer with OFFSET 300
                            End If

                            If (CInt(event_properties(k).event_code) = event_code) Then
                                Try
                                    My.Computer.Audio.Play(startup_path & "\config\" & event_properties(k).audio_file)
                                Catch ex As Exception
                                End Try
                                EventQueue.Enqueue("На об'єкті " & object_properties(i).name & " (" & object_properties(i).notes & ") відбуласа подія: " & event_properties(k).description & ", отримано в " & Now().ToString("H:mm:ss dd/MM/yyyy"))
                                AppendLog("Отримано повідомлення від об'єкту " & object_properties(i).name & " (IMEI=" & object_properties(i).imei & "), подія: " & event_properties(k).description)
                                k = num_event
                            End If
                        Next




                        'if oko-s2 protocol {866104020810332,F9,0B,05C8,1F,3.0.3,OKO-S2}
                        If (protocol_type = OKO_S2) Then
                            state = hex_8bits(fields(2))
                            ohrana = CBool(state.Substring(1, 1)) 'arm state
                            output(1) = CBool(state.Substring(2, 1)) 'out2 state
                            output(0) = CBool(state.Substring(3, 1)) 'out1 state
                            lowbat = Not CBool(state.Substring(4, 1)) 'low ACC state
                            est220 = CBool(state.Substring(5, 1)) '220 state
                            input(1) = CBool(state.Substring(6, 1)) 'in2 state
                            input(0) = CBool(state.Substring(7, 1)) 'in1 state
                        End If

                        'if oko-u2/pro protocol {863591021960863,00F9,5600,2F202000,0000,12,1A80808080808080,0000CCF10000000080CA1A7D0000B9DB00000000000000000000000000000000,072B06DE0000000000000FFB0FF90DEB0FFE0FFE0FFE0FFE0FFE0FFE0FFE0FFE,1.2.3,OKO-PRO,,,,,,,,,000007,}
                        If (protocol_type = OKO_U2_PRO) Then

                            If (Not fields(2).Substring(2, 2).Equals("00")) Then ohrana = True 'arm state
                            state = hex_8bits(fields(3).Substring(2, 2))
                            input(7) = CBool(state.Substring(0, 1)) 'in8 state
                            input(6) = CBool(state.Substring(1, 1)) 'in7 state
                            input(5) = CBool(state.Substring(2, 1)) 'in6 state
                            input(4) = CBool(state.Substring(3, 1)) 'in5 state
                            input(3) = CBool(state.Substring(4, 1)) 'in4 state
                            input(2) = CBool(state.Substring(5, 1)) 'in3 state
                            input(1) = CBool(state.Substring(6, 1)) 'in2 state
                            input(0) = CBool(state.Substring(7, 1)) 'in1 state

                            state = hex_8bits(fields(3).Substring(0, 2))
                            lowbat = Not CBool(state.Substring(4, 1)) 'low ACC state
                            est220 = CBool(state.Substring(5, 1)) '220 state

                            state = hex_8bits(fields(4).Substring(2, 2))
                            output(7) = CBool(state.Substring(0, 1)) 'out8 state
                            output(6) = CBool(state.Substring(1, 1)) 'out7 state
                            output(5) = CBool(state.Substring(2, 1)) 'out6 state
                            output(4) = CBool(state.Substring(3, 1)) 'out5 state
                            output(3) = CBool(state.Substring(4, 1)) 'out4 state
                            output(2) = CBool(state.Substring(5, 1)) 'out3 state
                            output(1) = CBool(state.Substring(6, 1)) 'out2 state
                            output(0) = CBool(state.Substring(7, 1)) 'out1 state

                        End If


                        'if oko-u protocol  {UD123456789098765,FB,С0,02,7А,5C,27,-10,-5,25,40,32,3,19,128,15grn,3.9}
                        If (protocol_type = OKO_U) Then
                            state = hex_8bits(fields(2))
                            ohrana = CBool(state.Substring(0, 1)) 'arm state
                            est220 = CBool(state.Substring(1, 1)) '220 state
                            lowbat = CBool(state.Substring(2, 1)) 'low ACC state

                            output(0) = CBool(state.Substring(5, 1)) 'out1 state
                            output(1) = CBool(state.Substring(6, 1)) 'out2 state
                            output(2) = CBool(state.Substring(7, 1)) 'out3 state

                            'inputs state
                            state = hex_8bits(fields(3))
                            input(3) = CBool(state.Substring(4, 1)) 'in4 state
                            input(2) = CBool(state.Substring(5, 1)) 'in3 state
                            input(1) = CBool(state.Substring(6, 1)) 'in2 state
                            input(0) = CBool(state.Substring(7, 1)) 'in1 state

                        End If



                        If (Not object_properties(i).connected) Then
                            object_properties(i).connected = True
                            Update_No_connectionGUI()
                        End If

                        If (Not object_properties(i).view) Then
                            If (view_time = 1) Then TableLayoutPanel_object.Controls.Add(BtnArray_time(i), 0, i)
                            If (view_arm = 1) Then TableLayoutPanel_object.Controls.Add(BtnArray_arm(i), view_time + 1, i)
                            If (view_220 = 1) Then TableLayoutPanel_object.Controls.Add(BtnArray_220(i), view_time + 1 + view_arm, i)
                            For k = 0 To num_input - 1
                                TableLayoutPanel_object.Controls.Add(BtnArray_input(k, i), view_time + 1 + view_arm + view_220 + k, i)
                            Next
                            For k = 0 To num_output - 1
                                TableLayoutPanel_object.Controls.Add(BtnArray_output(k, i), view_time + 1 + view_arm + view_220 + num_input + k, i)
                            Next
                            object_properties(i).view = True
                        End If

                        'update time
                        If (view_time = 1) Then BtnArray_time(i).Text = Now().ToString("dd/MM/yy H:mm:ss")

                        'update object button colour
                        BtnArray_object(i).BackColor = Color.LightGreen


                        'update arm state
                        If (view_arm = 1) Then
                            If (ohrana) Then
                                BtnArray_arm(i).BackgroundImage = Image.FromFile(startup_path & "\config\" & ARMED & ".jpg")
                                BtnArray_arm(i).Tag = True
                            Else
                                BtnArray_arm(i).BackgroundImage = Image.FromFile(startup_path & "\config\" & DISARMED & ".jpg")
                                BtnArray_arm(i).Tag = False
                            End If
                        End If


                        'update power state
                        If (view_220 = 1) Then
                            If (est220) Then
                                BtnArray_220(i).BackgroundImage = Image.FromFile(startup_path & "\config\" & _220_ON & ".jpg")
                            Else
                                If (lowbat) Then
                                    BtnArray_220(i).BackgroundImage = Image.FromFile(startup_path & "\config\" & LOW_ACC & ".jpg")
                                Else
                                    BtnArray_220(i).BackgroundImage = Image.FromFile(startup_path & "\config\" & _220_OFF & ".jpg")
                                End If
                            End If
                        End If


                        'update input state
                        For k = 0 To num_input - 1
                            If (input(k)) Then
                                BtnArray_input(k, i).BackgroundImage = Image.FromFile(startup_path & "\config\" & INPUT_TREVOGA & CStr(k + 1) & ".jpg")
                            Else
                                BtnArray_input(k, i).BackgroundImage = Image.FromFile(startup_path & "\config\" & INPUT_NORMA & CStr(k + 1) & ".jpg")
                            End If
                        Next

                        'update output state
                        For k = 0 To num_output - 1
                            If (output(k)) Then
                                BtnArray_output(k, i).BackgroundImage = Image.FromFile(startup_path & "\config\" & OUTPUT_ON & CStr(k + 1) & ".jpg")
                                BtnArray_output(k, i).Tag = True
                            Else
                                BtnArray_output(k, i).BackgroundImage = Image.FromFile(startup_path & "\config\" & OUTPUT_OFF & CStr(k + 1) & ".jpg")
                                BtnArray_output(k, i).Tag = False
                            End If
                        Next



                    Else
                        AppendInfo("Об'єкт " & object_properties(i).name & " має невідомий протокол. Пакет {" & data & "}.")

                    End If

                    i = num_object
                End If

            Next

            If (i = num_object) Then
                'MsgBox("Об'єкт з IMEI=" & fields(0) & " не зареєстровано в конфігураційному файлі object.txt")
                AppendInfo("Об'єкт з IMEI=" & fields(0) & " не зареєстровано в конфігураційному файлі object.txt.")

            End If

        Catch ex As Exception
            MessageBox.Show(ex.ToString)
        End Try
    End Sub



    Private Sub RemoveClientGUI(imei As String)
        Try
            Dim i As Integer
            For i = 0 To num_object - 1
                If (object_properties(i).imei.Equals(imei)) Then
                    object_properties(i).connected = False
                    BtnArray_object(i).BackColor = Color.Pink
                    Update_No_connectionGUI()
                End If
            Next
        Catch ex As Exception
            MessageBox.Show(ex.ToString)
        End Try
    End Sub

    Private Sub doGUI(data As mesage_properties_str)
        Try
            If (data.options = REMOVE_CLIENT_GUI) Then RemoveClientGUI(data.message)
            If (data.options = APPEND_OUTPUT) Then AppendLog(data.message)
            If (data.options = REFRESH_GUI) Then RefreshGUI(data.message)
            If (data.options = APPEND_INFO) Then AppendInfo(data.message)

        Catch ex As Exception
            MessageBox.Show(ex.ToString)
        End Try

    End Sub

    Private Sub messageReceived(ByVal sender As ConnectedClient, ByVal message As String, ByVal remove_client As Boolean, ByVal imei As String)
        Try
            Dim data(4) As Object
            data(0) = sender
            data(1) = message
            data(2) = remove_client
            data(3) = imei
            DataQueue.Enqueue(data)
        Catch ex As Exception
            MessageBox.Show(ex.ToString)
        End Try

    End Sub

    Private Sub messageReceived_(ByVal sender As ConnectedClient, ByVal message As String, ByVal remove_client As Boolean, ByVal imei As String)
        Try
            Dim unknown_packet = False

            Dim data As mesage_properties_str 'message for GUI

            If (remove_client) Then
                data.message = imei
                data.options = REMOVE_CLIENT_GUI
                doGUI(data) 
                RemoveClient(sender)
            Else

                'logging 
                data.message = message
                data.options = APPEND_OUTPUT
                doGUI(data) 

                Dim parts_ As String() = Strings.Split(message, "{")
                If parts_.Length = 2 Then
                    Dim parts As String() = Strings.Split(parts_(1), "}")
                    If parts.Length = 2 Then
                        message = "{" & parts(0) & "}"
                        Dim fields As String() = Strings.Split(parts(0), ",")
                        If fields.Length > 1 Then
                            sender.imei = fields(0)
                            data.message = parts(0)
                            data.options = REFRESH_GUI
                            doGUI(data) 
                        Else
                            unknown_packet = True
                        End If
                    Else
                        unknown_packet = True
                    End If
                Else
                    unknown_packet = True
                End If

                If (unknown_packet) Then
                    data.message = "Невідомий формат пакету: " + message
                    data.options = APPEND_INFO
                    doGUI(data) 
                End If

            End If

        Catch ex As Exception
            MessageBox.Show(ex.ToString)
        End Try
    End Sub



    Private Function GetClientByIMEI(ByVal name As String) As ConnectedClient
        Try
            For Each cc As ConnectedClient In clients
                If cc.imei = name Then
                    Return cc 'client found, return it.
                End If
            Next
            'If we've reached this part of the method, there is no client by that name
        Catch ex As Exception
            MessageBox.Show(ex.ToString)
        End Try
        Return Nothing
    End Function





    Private Sub ToolStripButton_log_Click(sender As Object, e As EventArgs) Handles ToolStripButton_log.Click
        Form_log.Show()
    End Sub


    Private Sub open_config_file()
        Try


            'load config info - if exist
            Dim curr_line As String
            f_r_name = startup_path & "\config\config.txt"
            fs = CreateObject("Scripting.FileSystemObject")
            f = fs.GetFile(f_r_name)
            'open source file
            ts = f.OpenAsTextStream(ForReading, TristateUseDefault)

            While ts.AtEndOfStream <> True
                curr_line = ts.ReadLine
                If (Mid(curr_line, 1, 1) <> "#") Then
                    curr_line = curr_line.Replace(" ", "")
                    Dim parts As String() = Strings.Split(curr_line, "=")  'ans1.Split(New [Char]() { CChar(vbTab), CChar(" "), CChar(";") })
                    If parts.Length = 2 Then
                        If parts(0).Equals("MAX_OBJECT") Then
                            max_num_object = CInt(parts(1))
                        End If
                        If parts(0).Equals("MAX_INPUT") Then
                            num_input = CInt(parts(1))
                            If (num_input > 8 Or num_input < 0) Then num_input = 8
                        End If
                        If parts(0).Equals("MAX_OUTPUT") Then
                            num_output = CInt(parts(1))
                            If (num_output > 8 Or num_output < 0) Then num_output = 8
                        End If
                        If parts(0).Equals("view_220") Then
                            view_220 = CInt(parts(1))
                            If (view_220 > 1 Or view_220 < 0) Then view_220 = 1
                        End If
                        If parts(0).Equals("view_time") Then
                            view_time = CInt(parts(1))
                            If (view_time > 1 Or view_time < 0) Then view_time = 1
                        End If
                        If parts(0).Equals("view_arm") Then
                            view_arm = CInt(parts(1))
                            If (view_arm > 1 Or view_arm < 0) Then view_arm = 1
                        End If
                        If parts(0).Equals("object_row_height") Then
                            object_row_height = CInt(parts(1))
                            If (object_row_height > 500 Or object_row_height < 50) Then object_row_height = 60
                        End If

                        If parts(0).Equals("save_log") Then
                            save_log = CInt(parts(1))
                            If (save_log > 1 Or save_log < 0) Then save_log = 1
                        End If

                        If parts(0).Equals("msg_event") Then
                            msg_event = CInt(parts(1))
                            If (msg_event > 1 Or msg_event < 0) Then msg_event = 1
                        End If


                    End If


                End If
            End While


        Catch ex As Exception
            MessageBox.Show(ex.ToString)
        End Try
    End Sub


    Private Sub open_object_file()
        Try

            num_object = 0

            'load config info - if exist
            Dim curr_line As String
            f_r_name = startup_path & "\config\object.txt"
            fs = CreateObject("Scripting.FileSystemObject")
            f = fs.GetFile(f_r_name)
            'open source file
            ts = f.OpenAsTextStream(ForReading, TristateUseDefault)

            While ts.AtEndOfStream <> True
                curr_line = ts.ReadLine
                If (Mid(curr_line, 1, 1) <> "#") Then

                    Dim parts As String() = Strings.Split(curr_line, ";")  'ans1.Split(New [Char]() { CChar(vbTab), CChar(" "), CChar(";") })
                    If parts.Length = 23 Then
                        num_object += 1
                        If (num_object < max_num_object + 1) Then
                            ReDim Preserve object_properties(num_object)
                            object_properties(num_object - 1).name = parts(0)
                            object_properties(num_object - 1).notes = parts(1)
                            object_properties(num_object - 1).imei = parts(2)
                            object_properties(num_object - 1).code = parts(3)
                            object_properties(num_object - 1).arm = parts(4)
                            object_properties(num_object - 1).disarm = parts(5)
                            object_properties(num_object - 1).out1_on = parts(6)
                            object_properties(num_object - 1).out1_off = parts(7)
                            object_properties(num_object - 1).out2_on = parts(8)
                            object_properties(num_object - 1).out2_off = parts(9)
                            object_properties(num_object - 1).out3_on = parts(10)
                            object_properties(num_object - 1).out3_off = parts(11)
                            object_properties(num_object - 1).out4_on = parts(12)
                            object_properties(num_object - 1).out4_off = parts(13)
                            object_properties(num_object - 1).out5_on = parts(14)
                            object_properties(num_object - 1).out5_off = parts(15)
                            object_properties(num_object - 1).out6_on = parts(16)
                            object_properties(num_object - 1).out6_off = parts(17)
                            object_properties(num_object - 1).out7_on = parts(18)
                            object_properties(num_object - 1).out7_off = parts(19)
                            object_properties(num_object - 1).out8_on = parts(20)
                            object_properties(num_object - 1).out8_off = parts(21)
                            object_properties(num_object - 1).time_out_len = CInt(parts(22))
                            object_properties(num_object - 1).time_out = 0 'CInt(parts(22))
                            object_properties(num_object - 1).connected = False
                            object_properties(num_object - 1).view = False

                        End If
                    End If
                End If
            End While


        Catch ex As Exception
            MessageBox.Show(ex.ToString)
        End Try
    End Sub


    Private Sub open_event_file()
        Try

            num_event = 0

            'load config info - if exist
            Dim curr_line As String
            f_r_name = startup_path & "\config\event.txt"
            fs = CreateObject("Scripting.FileSystemObject")
            f = fs.GetFile(f_r_name)
            'open source file
            ts = f.OpenAsTextStream(ForReading, TristateUseDefault)

            While ts.AtEndOfStream <> True
                curr_line = ts.ReadLine
                If (Mid(curr_line, 1, 1) <> "#") Then

                    Dim parts As String() = Strings.Split(curr_line, ";")  'ans1.Split(New [Char]() { CChar(vbTab), CChar(" "), CChar(";") })
                    If parts.Length = 3 Then   'like this 301;alarm.wav;ПОРУШЕННЯ дротової зони-1
                        num_event += 1
                        ReDim Preserve event_properties(num_event)
                        event_properties(num_event - 1).event_code = parts(0)
                        event_properties(num_event - 1).audio_file = parts(1)
                        event_properties(num_event - 1).description = parts(2)
                    End If
                End If
            End While


        Catch ex As Exception
            MessageBox.Show(ex.ToString)
        End Try
    End Sub

    Private Sub ToolStripButton_exit_Click(sender As Object, e As EventArgs) Handles ToolStripButton_exit.Click
        AppendLog("Натиснуто кнопку ВИХІД.")
        exit_program = True
    End Sub

    Private Sub Form1_Closing(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.FormClosing

        Try
            Dim Message As String = Now().ToString("dd/MM/yy H:mm:ss") & "-->" & "Закрито програму - натиснуто на хрестик." & ControlChars.NewLine

            If (save_log = 1) Then
                Dim Path As String
                Path = startup_path & "\history\" & Now().ToString("yyyy_MM_dd") & "_log.txt"

                ' This text is added only once to the file. 
                If Not File.Exists(Path) Then
                    ' Create a file to write to.
                    Using sw As StreamWriter = File.CreateText(Path)
                        sw.WriteLine(Now().ToString("dd/MM/yy H:mm:ss") & "-->СТВОРЕННЯ ФАЙЛУ")
                    End Using
                End If

                ' This text is always added, making the file longer over time 
                ' if it is not deleted.
                Using sw As StreamWriter = File.AppendText(Path)
                    sw.Write(Message)
                End Using
            End If
        Catch ex As Exception
            MessageBox.Show(ex.ToString)
        End Try
    End Sub


    Private Sub Timer_check_data_Tick(sender As Object, e As EventArgs) Handles Timer_check_data.Tick
        Try
            Static Dim count As Integer = 0

            Dim data(4) As Object
            If (DataQueue.Count > 0) Then
                data = DataQueue.Dequeue()
                messageReceived_(data(0), data(1), data(2), data(3))
            End If
            Me.ToolStripStatusLabel1.Text = String.Format("{0} з'єднань", clients.Count)
            Me.Label_test.Text = "check_data: " & count
            'count += 1
        Catch ex As Exception
            MessageBox.Show(ex.ToString)
        End Try
    End Sub

    Private Sub Timer_periodic_object_check_Tick(sender As Object, e As EventArgs) Handles Timer_periodic_object_check.Tick
        Try
            Me.Label_test.Text = "check_periodic_object"
            Dim i As Integer

            For i = 0 To num_object - 1
                If (object_properties(i).time_out > 0) Then
                    object_properties(i).time_out = object_properties(i).time_out - 1
                    If (object_properties(i).time_out = 0) Then
                        If (msg_event = 1) Then
                            Try
                                My.Computer.Audio.Play(startup_path & "\config\message.wav")
                            Catch ex As Exception
                            End Try
                            AppendLog("Виведено повідомлення про подію: " & "Відсутні дані від об'єкту " & object_properties(i).name & " (IMEI=" & object_properties(i).imei & ") напротязі " & object_properties(i).time_out_len & " хвилин.") 'save to log
                            MsgBox("Відсутні дані від об'єкту " & object_properties(i).name & " (" & object_properties(i).notes & ") напротязі " & object_properties(i).time_out_len & " хвилин.", MsgBoxStyle.SystemModal)   'show event message on top
                            AppendLog("Закриття повідомлення про подію: " & "Відсутні дані від об'єкту " & object_properties(i).name & " (IMEI=" & object_properties(i).imei & ") напротязі " & object_properties(i).time_out_len & " хвилин.") 'save to log
                        End If
                    End If
                End If
            Next

        Catch ex As Exception
            MessageBox.Show(ex.ToString)
        End Try
    End Sub

    Private Sub Timer_save_log_Tick(sender As Object, e As EventArgs) Handles Timer_save_log.Tick
        Try
            Dim message As String
            If Not bw_save_log.IsBusy = True Then
                If (LogQueue.Count > 0) Then
                    message = LogQueue.Dequeue() 'get data from queue
                    'save to log file
                    If (save_log = 1) Then
                        Me.Label_test.Text = "save_log"
                        bw_save_log.RunWorkerAsync(message)
                    End If
                Else
                    If (exit_program) Then End
                End If
            End If
        Catch ex As Exception
            MessageBox.Show(ex.ToString)
        End Try
    End Sub
    Private Sub bw_save_log_DoWork(ByVal sender As Object, ByVal e As DoWorkEventArgs)
        Try
            Dim Path As String
            Path = startup_path & "\history\" & Now().ToString("yyyy_MM_dd") & "_log.txt"

            ' This text is added only once to the file. 
            If Not File.Exists(Path) Then
                ' Create a file to write to.
                Using sw As StreamWriter = File.CreateText(Path)
                    sw.WriteLine(Now().ToString("dd/MM/yy H:mm:ss") & "-->СТВОРЕННЯ ФАЙЛУ")
                End Using
            End If

            ' This text is always added, making the file longer over time 
            ' if it is not deleted.
            Using sw As StreamWriter = File.AppendText(Path)
                sw.Write(e.Argument)
            End Using
        Catch ex As Exception
            MessageBox.Show(ex.ToString)
        End Try
        e.Result = "save_log done"
    End Sub

    Private Sub bw_save_log_RunWorkerCompleted(ByVal sender As Object, ByVal e As RunWorkerCompletedEventArgs)
        Try
            If (e.Error IsNot Nothing) Then
                MessageBox.Show("Error: " & e.Error.Message)
            Else
                Me.Label_test.Text = e.Result
            End If

        Catch ex As Exception
            MessageBox.Show(ex.ToString)
        End Try
    End Sub

    Private Sub Timer_event_object_check_Tick(sender As Object, e As EventArgs) Handles Timer_event_object_check.Tick
        Try
            If (EventQueue.Count > 0) Then '
                Dim event_description As String = EventQueue.Dequeue() 'get data from queue
                If (msg_event = 1) Then
                    AppendLog("Виведено повідомлення про подію: " & event_description) 'save to log
                    MsgBox(event_description, MsgBoxStyle.SystemModal, "Увага! Подія з об'єкту")   'show event message on top
                    AppendLog("Закриття повідомлення про подію: " & event_description) 'save to log
                End If
            End If

        Catch ex As Exception
            MessageBox.Show(ex.ToString)
        End Try
    End Sub
End Class

'client side

Public Class ConnectedClient

    Private mClient As System.Net.Sockets.TcpClient



    Private mIMEI As String

    Private mParentForm As Form_main

    Private readThread As System.Threading.Thread

    Private Const MESSAGE_DELIMITER As Char = ControlChars.Cr



    Public Event dataReceived(ByVal sender As ConnectedClient, ByVal message As String, ByVal remove_client As Boolean, ByVal imei As String)



    Sub New(ByVal client As System.Net.Sockets.TcpClient, ByVal parentForm As Form_main)

        mParentForm = parentForm

        mClient = client


        readThread = New System.Threading.Thread(AddressOf doRead)

        readThread.IsBackground = True

        readThread.Start()

    End Sub



    Public Property imei() As String

        Get

            Return mIMEI

        End Get

        Set(ByVal value As String)

            mIMEI = value

        End Set

    End Property



    Private Sub doRead()
        Try
            Const BYTES_TO_READ As Integer = 512 '255

            Dim readBuffer(BYTES_TO_READ) As Byte

            Dim bytesRead As Integer

            Dim sBuilder As New System.Text.StringBuilder
            Dim need_close_socket = False
            Do
                mClient.ReceiveTimeout = 720000

                Try
                    bytesRead = mClient.GetStream.Read(readBuffer, 0, BYTES_TO_READ)

                Catch ex As Exception

                    'MessageBox.Show(ex.ToString)
                    need_close_socket = True
                End Try

                If (bytesRead > 0) And Not need_close_socket Then

                    Dim message As String = System.Text.Encoding.UTF8.GetString(readBuffer, 0, bytesRead)

                    If (message.IndexOf(MESSAGE_DELIMITER) > -1) Then



                        Dim subMessages() As String = message.Split(MESSAGE_DELIMITER)



                        'The first element in the subMessages string array must be the last part of the current message.

                        'So we append it to the StringBuilder and raise the dataReceived event

                        sBuilder.Append(subMessages(0))

                        RaiseEvent dataReceived(Me, sBuilder.ToString, False, imei)

                        sBuilder = New System.Text.StringBuilder



                        'If there are only 2 elements in the array, we know that the second one is an incomplete message,

                        'though if there are more then two then every element inbetween the first and the last are complete messages:

                        If subMessages.Length = 2 Then

                            sBuilder.Append(subMessages(1))

                        Else

                            For i As Integer = 1 To subMessages.GetUpperBound(0) - 1

                                RaiseEvent dataReceived(Me, subMessages(i), False, imei)

                            Next

                            sBuilder.Append(subMessages(subMessages.GetUpperBound(0)))

                        End If

                    Else



                        'MESSAGE_DELIMITER was not found in the message, so we just append everything to the stringbuilder.

                        sBuilder.Append(message)

                    End If

                Else
                    need_close_socket = True
                End If

            Loop While Not need_close_socket

            CloseSocket()

        Catch ex As Exception

            MessageBox.Show(ex.ToString)

        End Try

        Return

    End Sub

    Public Sub CloseSocket()
        Try
            RaiseEvent dataReceived(Me, "", True, imei)
            mClient.Close()

        Catch ex As Exception

            'MessageBox.Show(ex.ToString)

        End Try
    End Sub

    Public Sub SendMessage(ByVal msg As String)

        Dim sw As IO.StreamWriter

        Try

            SyncLock mClient.GetStream

                sw = New IO.StreamWriter(mClient.GetStream) 'Create a new streamwriter that will be writing directly to the networkstream.

                sw.Write(msg)

                sw.Flush()

            End SyncLock

        Catch ex As Exception

            MessageBox.Show(ex.ToString)

        End Try

        'As opposed to writing to a file, we DONT call close on the streamwriter, since we dont want to close the stream.

    End Sub



End Class

