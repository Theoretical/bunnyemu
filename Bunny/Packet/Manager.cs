using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Bunny.Core;
using Bunny.Enums;

namespace Bunny.Packet
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    class PacketHandlerAttribute : Attribute
    {

        public PacketHandlerAttribute(Operation pOpcode, PacketFlags pFlags)
        {
            Opcode = pOpcode;
            Flag = pFlags;
        }
        public Operation Opcode
        {
            get { return _operation; }
            set { _operation = value; }
        }

        public PacketFlags Flag
        {
            get { return _flags; }
            set { _flags = value; }
        }
        private Operation _operation;
        private PacketFlags _flags;
    }

    class HandlerDelegate
    {
        public delegate void PacketProcessor(Client client, PacketReader packetReader);

        public PacketProcessor Processor;
        public PacketFlags Flags = PacketFlags.Login;

        public HandlerDelegate(PacketProcessor pAction, PacketFlags pFlags)
        {
            Processor = pAction;
            Flags = pFlags;
        }
    }

    class Manager
    {
        public static Dictionary<Operation, HandlerDelegate> Operations = new Dictionary<Operation, HandlerDelegate>();

        // <summary>
        // Loads the packet handles of class T1.
        // </summary>
        public static void InitializeHandlers<T1>()
        {
            var methods = typeof(T1).GetMethods(BindingFlags.Static | BindingFlags.Public);
            foreach (var method in methods)
            {
                var attributes = method.GetCustomAttributes(typeof(PacketHandlerAttribute), false);

                if (attributes.Length != 1)
                    continue;

                var attribute = (PacketHandlerAttribute)attributes[0];
                if (Operations.ContainsKey(attribute.Opcode))
                    continue;
                Operations.Add(attribute.Opcode, new HandlerDelegate(new HandlerDelegate.PacketProcessor((Action<Client, PacketReader>)Delegate.CreateDelegate(typeof(Action<Client, PacketReader>), method)), attribute.Flag));
            }
        }
    }
}
