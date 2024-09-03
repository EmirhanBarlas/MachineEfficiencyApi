using Microsoft.AspNetCore.Mvc;
using System.IO.Ports;

namespace SerialPortApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SerialPortController : ControllerBase
    {
        private readonly SerialPort _serialPort;

        public SerialPortController()
        {
            _serialPort = new SerialPort
            {
                PortName = "COM4",
                BaudRate = 9600,
                DataBits = 7,
                StopBits = StopBits.One,
                Parity = Parity.Even,
                ReadTimeout = 500,
                WriteTimeout = 500
            };

            _serialPort.Open();
        }

        ~SerialPortController()
        {
            _serialPort.Close();
        }

        [HttpGet("R00001")]
        public IActionResult GetR00001()
        {
            return GetMachineEfficiency("R00001");
        }

        [HttpGet("R00000")]
        public IActionResult GetR00000()
        {
            return GetMachineEfficiency("R00000");
        }

        private IActionResult GetMachineEfficiency(string machineId)
        {
            try
            {
                var data = $"a01" + "4601" + machineId;
                var ascii = new List<byte>(System.Text.Encoding.ASCII.GetBytes(data));
                ascii[0] = 0x02;
                var lrc = CalculateLRC(ascii);
                ascii.AddRange(System.Text.Encoding.ASCII.GetBytes(lrc));
                ascii.Add(0x03);
                var sendData = ascii.ToArray();

                _serialPort.Write(sendData, 0, sendData.Length);
                var inputData = _serialPort.ReadTo(Convert.ToString((Char)3));
                var first = inputData.IndexOf("460") + 3;
                var i = Int32.Parse(inputData.Substring(first, 4), System.Globalization.NumberStyles.HexNumber);

                return Ok(new MachineEfficiencyOutput
                {
                    Machine = machineId,
                    Efficiency = i
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        private string CalculateLRC(List<byte> bytes)
        {
            int lrc = 0;
            foreach (var b in bytes)
            {
                lrc = (byte)((lrc + b) & 0xFF);
            }
            return lrc.ToString("X");
        }
    }

    public class MachineEfficiencyOutput
    {
        public string Machine { get; set; }
        public int Efficiency { get; set; }
    }
}