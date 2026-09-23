
using System;

using System.IO.Ports;
using System.Linq;

using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Modul_3.Services
{
    public class ArduinoService : IDisposable
    {
        private SerialPort _serialPort;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isConnected;

      //  private int nC; // Задел на динамическое определение 

        //private ContactMarker _contactMarker; //То, по чему будет определяться динамическое определение. Масло масляное л ляляляляля

        public event Action<bool[]> ContactsStateChanged;
        public event Action<string> MessageReceived;

        

        private readonly StringBuilder _pendingBuffer = new StringBuilder();

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();

            if (_serialPort != null)
            {
                if (_serialPort.IsOpen)
                {
                    _serialPort.Close();
                }
                _serialPort.Dispose();
                _serialPort = null;
            }
        }

        public bool IsConnected => _isConnected;

        public string[] AvailablePorts => SerialPort.GetPortNames();
        //Подключение и автоматический поиск порта
        public async Task<bool> ConnectAsync(string portName)
        {
            try
            {
                Disconnect();
                _serialPort = new SerialPort(portName, 57600)
                {
                    ReadTimeout = 1000,
                    WriteTimeout = 1000,
                    DtrEnable = true,   
                };

                _serialPort.Open();
                _serialPort.DiscardInBuffer(); 

                _isConnected = true;
                

                await Task.Delay(2000);

                _cancellationTokenSource = new CancellationTokenSource();
                _ = Task.Run(() => ReadDataAsync(_cancellationTokenSource.Token));
                _serialPort.Write("puk:0");
                var nado = _serialPort.ReadLine();
                if (!string.IsNullOrEmpty(nado))
                {
                    if (nado.StartsWith("kak:1"))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
                
            }
            catch (Exception ex)
            {
                MessageReceived?.Invoke($"Connection error: {ex.Message}");
                _isConnected = false;
                return false;
            }
        }

        public void Disconnect()
        {
            _cancellationTokenSource?.Cancel();

            if (_serialPort?.IsOpen == true)
            {
                StopContinuousReading();
                _serialPort.Close();
            }

            _serialPort?.Dispose();
            _serialPort = null;
            _isConnected = false;
        }

        public void StartContinuousReading(string command = "rpn:12")
        {
      ////      if (IsConnected && (product_name=="Ш5"|| product_name == "Ш6"))
      //      {                
      //           _serialPort?.WriteLine("tr:37"); //  $"Start_ {_contactMarker.ContactNumber.ToString()}" (Для динамической подстройки)
      //      }
            if (IsConnected)
            {
                MessageReceived?.Invoke($"Message incoming{command}");
                _serialPort?.WriteLine(command); //  $"Start_ {_contactMarker.ContactNumber.ToString()}" (Для динамической подстройки)
            }
        }
        //Очистка буфера для обработки дефектов
        public void DiscardBuffers()
        {
            if (_serialPort?.IsOpen == true)
            {
                _serialPort.DiscardInBuffer();
                _serialPort.DiscardOutBuffer();
            }
        }



        public void StopContinuousReading()
        {
            if (IsConnected)
            {
                _serialPort?.WriteLine(" ");
            }
        }
        //Чтение полученной информации.
        private async Task ReadDataAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _serialPort?.IsOpen == true)
            {
                try
                {
                    int bytesToRead = _serialPort.BytesToRead;
                    if (bytesToRead > 0)
                    {
                        byte[] buffer = new byte[bytesToRead];
                        _serialPort.Read(buffer, 0, bytesToRead);
                        string chunk = Encoding.ASCII.GetString(buffer);

                        _pendingBuffer.Append(chunk);

                        string full = _pendingBuffer.ToString();
                        int newlineIndex;
                        while ((newlineIndex = full.IndexOf('\n')) >= 0)
                        {
                            string line = full.Substring(0, newlineIndex).Trim();
                            _pendingBuffer.Remove(0, newlineIndex + 1);

                            if (!string.IsNullOrEmpty(line))
                                ProcessReceivedData(line);

                            full = _pendingBuffer.ToString();
                        }
                    }
                    else
                    {
                        await Task.Delay(10, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    MessageReceived?.Invoke($"Read error: {ex.Message}");
                    break;
                }
            }
        }
        public void ClearPendingBuffer()
        {
            lock (_pendingBuffer) { _pendingBuffer.Clear(); }
            DiscardBuffers();
        }
        //Обработка полученного значения
        private void ProcessReceivedData(string data) //Для обработки уничерсального сообщения (и 10 и 35), разобраться потом!!!!
        {
            data = data.Trim();
            if (string.IsNullOrEmpty(data)) return;

            string[] stateValues;
            if (data.Contains(' '))
                stateValues = data.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            else
                stateValues = data.Select(c => c.ToString()).ToArray();

            // Обрабатываем любую длину, если все символы 0 или 1
            if (stateValues.Length > 0 && stateValues.All(c => c == "0" || c == "1"))
            {
                bool[] states = stateValues.Select(c => c == "0").ToArray();
                ContactsStateChanged?.Invoke(states);
            }
        }
        //else if (data.StartsWith("PIN"))
        //{
        //    // Обработка отдельных пинов
        //}
    }
    }
