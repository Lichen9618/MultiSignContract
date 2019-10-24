using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.Numerics;

namespace MultiSign
{
    public class MultiSign : SmartContract
    {
        public static event Action<bool> updateWhiteList_event;
        public static event Action<BigInteger> nonce_event;

        public static object Main(string operation, object[] args, object[] signs)
        {
            if (operation == "Deploy") return Deploy(args);
            if (operation == "GetNonce") return GetNonce();
            if (operation == "GetWhiteList")
            {
                if (GetNonce() == 0)
                {
                    return false;
                }
                else
                {
                    return GetWhiteList();
                }
            }
            if (operation == "UpdateWhiteList") return UpdateWhiteList(args, signs);
            if (operation == "CheckMultisign") return CheckMultisign(args.Serialize(), signs);
            if (operation == "Serialize") return args.Serialize();
            return false;
        }

        private static BigInteger GetNonce()
        {
            BigInteger Nonce = Storage.Get(Storage.CurrentContext, "Nonce").AsBigInteger();
            return Nonce;
        }

        private static Map<BigInteger, byte[]> GetWhiteList()
        {
            Map<BigInteger, byte[]> WhiteList = (Map<BigInteger, byte[]>)Storage.Get(Storage.CurrentContext, "WhiteList").Deserialize();
            return WhiteList;
        }

        private static bool Deploy(object[] args)
        {
            BigInteger Nonce = Storage.Get(Storage.CurrentContext, "Nonce").AsBigInteger();
            if (Nonce == 0 && (BigInteger)args[0] == 1)
            {
                StoreWhiteList(args);
                IncreaseNonce();
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool CheckMultisign(byte[] Message, object[] Signs)
        {
            int Count = 0;
            Map<BigInteger, byte[]> WhiteList = GetWhiteList();
            for (int i = 0; i < Signs.Length; i++)
            {
                for (int j = 1; j <= WhiteList.Keys.Length; j++)
                {
                    if (SmartContract.VerifySignature(Message, (byte[])Signs[i], WhiteList[j]))
                    {
                        Count++;
                    }
                }
            }
            Runtime.Notify(Count);
            //TODO: Count number should be defined by mixmarvel
            if (Count >= 2)
            {
                return true;
            }
            return false;
        }

        private static bool UpdateWhiteList(object[] args, object[] signs)
        {
            BigInteger Nonce = Storage.Get(Storage.CurrentContext, "Nonce").AsBigInteger();
            if ((Nonce + 1).Equals((BigInteger)args[0]))
            {
                if (CheckMultisign(args.Serialize(), signs))
                {
                    StoreWhiteList(args);
                    IncreaseNonce();
                    return true;
                }
            }
            updateWhiteList_event(false);
            return false;
        }

        private static void StoreWhiteList(object[] args)
        {
            Map<BigInteger, byte[]> WhiteList = new Map<BigInteger, byte[]>();
            int number = args.Length;
            for (int i = 1; i < number; i++)
            {
                WhiteList[i] = (byte[])args[i];
            }
            Runtime.Notify(WhiteList);
            Storage.Put(Storage.CurrentContext, "WhiteList", WhiteList.Serialize());
            updateWhiteList_event(true);
        }

        private static void IncreaseNonce()
        {
            BigInteger Temp = Storage.Get(Storage.CurrentContext, "Nonce").AsBigInteger();
            Temp = Temp + 1;
            Storage.Put(Storage.CurrentContext, "Nonce", Temp);
            nonce_event(Temp);
        }
    }
}
