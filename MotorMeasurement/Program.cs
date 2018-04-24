using System;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace MotorMeasurement
{
    class Program
    {
        static SerialPort port = null;
        static Thread readThread;
        static bool exit = false;

        static void Main(string[] args)
        {
            readThread = new Thread(Read);

            ////////////////////////////////////////////////////////////////////////////
            short[] stages = {50, 80, 50, 0 };
            int stageTime = 2000;
            int measurementNumber = 60;
            ///////////////////////////////////////////////////////////////////////////

            try
            {
                port = new SerialPort("COM4", 115200, Parity.None, 8, StopBits.One);
                port.ReadTimeout = 500;
                port.WriteTimeout = 500;
                port.Open();
                readThread.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.ReadKey();
                return;
            }

            if(false)
            {
                Thread.Sleep(1000);
                var payload = new RegulatorParametersPayload { Position_P = 50, Position_I = 100, Speed_P = 250, Speed_I = 300 };
                var packet = new Packet(MessageCode.REGULATOR_PARAMETERS_SET, Packet.StructToByteArray(payload));
                port.Write(packet.Data, 0, packet.Length);
            }

            if (true)
            {
                Thread.Sleep(1000);
                var payload = new MotorSetDistancePayload { Speed = 15, Distance = 45 };
                var packet = new Packet(MessageCode.REGULATED_ANGLE_SET, Packet.StructToByteArray(payload));

                for (int i = 0; i < 10; i++)
                {
                    port.Write(packet.Data, 0, packet.Length);
                   Thread.Sleep(1000);
                }
            }
            if (false)
            {

                Console.WriteLine("Press Enter to start");
                //////////////////////////////////////////////////////////////////////
                port.Write(new byte[] { 0xE1, 0x02, (byte)MessageCode.TESTING_MODE_ON, 0xFF }, 0, 4);  // turn on testing mode
                                                                                                       //////////////////////////////////////////////////////////////////////

                Console.ReadLine();
                ///////////////////////////////////////////////////////////////////////////////
                var payload = new MotorSetSpeedPayload { SpeedL = stages[0], SpeedR = stages[0] };
                var packet = new Packet(MessageCode.MOTOR_SET_SPEED, Packet.StructToByteArray(payload));
                port.Write(packet.Data, 0, packet.Length);
                ///////////////////////////////////////////////////////////////////////////////
                Thread.Sleep(stageTime);
                ///////////////////////////////////////////////////////////////////////////////
                payload = new MotorSetSpeedPayload { SpeedL = stages[1], SpeedR = stages[1] };
                packet = new Packet(MessageCode.MOTOR_SET_SPEED, Packet.StructToByteArray(payload));
                port.Write(packet.Data, 0, packet.Length);
                ///////////////////////////////////////////////////////////////////////////////
                Thread.Sleep(stageTime);
                ///////////////////////////////////////////////////////////////////////////////
                payload = new MotorSetSpeedPayload { SpeedL = stages[2], SpeedR = stages[2] };
                packet = new Packet(MessageCode.MOTOR_SET_SPEED, Packet.StructToByteArray(payload));
                port.Write(packet.Data, 0, packet.Length);
                ///////////////////////////////////////////////////////////////////////////////
                Thread.Sleep(stageTime);
                ///////////////////////////////////////////////////////////////////////////////
                payload = new MotorSetSpeedPayload { SpeedL = stages[3], SpeedR = stages[3] };
                packet = new Packet(MessageCode.MOTOR_SET_SPEED, Packet.StructToByteArray(payload));
                port.Write(packet.Data, 0, packet.Length);
                ///////////////////////////////////////////////////////////////////////////////
                Thread.Sleep(stageTime * 2);
                //////////////////////////////////////////////////////////////////////
                port.Write(new byte[] { 0xE1, 0x02, (byte)MessageCode.TESTING_MODE_OFF, 0xFF }, 0, 4); // turn off testing mode
                                                                                                       //////////////////////////////////////////////////////////////////////
                Thread.Sleep(50);
                exit = true;
                readThread.Join();

                Console.WriteLine("Press any key to exit");
                // parse file

                using (StreamReader file = new StreamReader("ReceivedData.txt"))
                using (StreamWriter outputFile = new StreamWriter(String.Format("Data{0}.m", measurementNumber)))
                {
                    var splitText = file.ReadToEnd().Split('-');

                    StringBuilder desiredvalue = new StringBuilder();
                    StringBuilder desiredvalueTime = new StringBuilder();
                    StringBuilder timeValue = new StringBuilder();

                    outputFile.WriteLine(String.Format("% measurement number {0}", measurementNumber));
                    outputFile.WriteLine(String.Format("% First stage:{0} \n% Second stage:{1} \n% Third stage:{2} \n% Stage time:{3} ", stages[0], stages[1], stages[2], stageTime));
                    outputFile.WriteLine("y = [");

                    int speedPacketNumber = 0;
                    int actualStage = 0;

                    for (int i = 0; i < splitText.Length; i++)
                    {
                        if (splitText[i] == "E1" && splitText[i + 1] == "03" && splitText[i + 2] == "10") //Ack packet 
                        {
                            if (actualStage < stages.Length - 1)
                                desiredvalue.Append(stages[actualStage] + " ");
                            desiredvalue.Append(stages[actualStage++] + " ");
                            desiredvalueTime.Append((speedPacketNumber / 100.0).ToString().Replace(',', '.') + " ");
                            if (actualStage > 1)
                                desiredvalueTime.Append((speedPacketNumber / 100.0).ToString().Replace(',', '.') + " ");
                        }
                        if (splitText[i] == "E1" && splitText[i + 1] == "04" && splitText[i + 2] == "32") // report speed packet
                        {
                            timeValue.Append((speedPacketNumber / 100.0).ToString().Replace(',', '.') + " ");
                            speedPacketNumber++;
                            outputFile.WriteLine(HexToSigned16(splitText[i + 3] + splitText[i + 4], true));
                        }
                    }
                    outputFile.WriteLine("];");
                    outputFile.WriteLine("u=[" + desiredvalue.ToString() + "];");
                    outputFile.WriteLine("u_t=[" + desiredvalueTime.ToString() + "];");
                    outputFile.WriteLine("y_t=[" + timeValue.ToString() + "];");
                    outputFile.WriteLine("plot(y_t,y) \nfigure \nplot(u_t,u)");

                }
            }
        }

        static short HexToSigned16(string value, bool isLittleEndian)
        {
            byte first = HexToByte(value, 0);
            byte second = HexToByte(value, 2);

            if (isLittleEndian)
                return (short)(first | (second << 8));
            else
                return (short)(second | (first << 8));
        }

        static byte HexToByte(string value, int offset)
        {
            string hex = value.Substring(offset, 2);
            return byte.Parse(hex, NumberStyles.HexNumber);
        }

        public static void Read()
        {
            //using (StreamWriter outputFile = new StreamWriter("ReceivedData.txt"))
            {
                while (!exit)
                {
                    Thread.Sleep(200);

                    int availableBytes = port.BytesToRead;
                    if (availableBytes > 0)
                    {
                        try
                        {
                            byte[] buffer = new byte[availableBytes];
                            port.Read(buffer, 0, availableBytes);
                            Console.WriteLine("Data: " + BitConverter.ToString(buffer));
                            //outputFile.Write(BitConverter.ToString(buffer));
                        }
                        catch (TimeoutException) { Console.WriteLine("[Timeout] Serial Read timeout"); }
                    }
                }
            }
        }
    }
}
