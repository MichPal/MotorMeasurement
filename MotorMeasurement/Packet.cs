using System;
using System.Runtime.InteropServices;

namespace MotorMeasurement
{
    public enum MessageCode : byte
    {
        GENERAL_ACK = 0x10,
        GENERAL_NACK_SEND_AGAIN = 0x02,

        MOTOR_SET_SPEED = 0x11,
        MOTOR_STOP = 0x12,

        DEVICE_GET_POSITION_REQUEST = 0x20,
        DEVICE_GET_POSITION_RESPONSE = 0x21,
        DEVICE_GET_SPEED_REQUEST = 0x22,
        DEVICE_GET_SPEED_RESPONSE = 0x23,
        DEVICE_GET_STATUS_REQUEST = 0x24,
        DEVICE_GET_STATUS_RESPONSE = 0x25,

        DEVICE_SET_POSITION = 0x26,

        TESTING_MODE_ON = 0x30,
        TESTING_MODE_OFF = 0x31,
        REPORT_CURRENT_SPEED = 0x32,
    }
    
    class Packet
    {
        private byte[] data;

        public MessageCode RequestMessageCode { get; private set; }
        public int Length { get => this.data.Length; }
        public byte[] Data { get => this.data; }

        private const int minimumDataLength = 3; //start,length,requestCode
        private const byte StartByte = 0xE1;

        public Packet(MessageCode requestCode, byte[] payload)
        {
            this.RequestMessageCode = requestCode;
            this.data = new byte[minimumDataLength + payload.Length];

            this.data[0] = StartByte;
            this.data[1] = (byte)(payload.Length + 1);
            this.data[2] = (byte)requestCode;
            Buffer.BlockCopy(payload, 0, this.data, 3, payload.Length);
        }

        public Packet(MessageCode requestCode)
        {
            this.RequestMessageCode = requestCode;
            this.data = new byte[minimumDataLength];

            this.data[0] = StartByte;
            this.data[1] = (byte)1;
            this.data[2] = (byte)requestCode;
        }

        public static byte[] StructToByteArray<T>(T s) where T : struct
        {
            int objsize = Marshal.SizeOf(typeof(T));
            Byte[] ret = new Byte[objsize];
            IntPtr buff = Marshal.AllocHGlobal(objsize);
            Marshal.StructureToPtr(s, buff, true);
            Marshal.Copy(buff, ret, 0, objsize);
            Marshal.FreeHGlobal(buff);
            return ret;
        }
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MotorSetSpeedPayload
    {
        public short SpeedL;
        public short SpeedR;
    }
}