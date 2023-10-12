using System;
using System.ComponentModel;
using System.Numerics;

using Neo;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;

namespace NFTAccountRegistry
{

    public class InitializeParams
    {
        public UInt160 NFTContract = UInt160.Zero;
        public ByteString TokenId = ByteString.Empty;
        public BigInteger Kind;
        public BigInteger Funny;
        public BigInteger Sad;
        public BigInteger Angry;
    }

    [DisplayName("Gabreiel.NFTAccountRegistryContract")]
    [ManifestExtra("Author", "Your name")]
    [ManifestExtra("Email", "your@address.invalid")]
    [ManifestExtra("Description", "Describe your contract...")]
    public class NFTAccountRegistryContract : SmartContract
    {
        const byte Prefix_NumberStorage = 0x00;
        const byte Prefix_AccountsStorage = 0x01;
        const byte Prefix_ContractOwner = 0xFF;

        // [InitialValue(""), ContractParameterType.UInt160]
        // static readonly UInt160 AccountImplementation = default;

        private static Transaction Tx => (Transaction)Runtime.ScriptContainer;

        public delegate void OnAccountCreatedDelegate(UInt160 nftScriptHash, UInt160 creator, ByteString tokenId, BigInteger salt);
        public delegate void OnPostedDelegate(ByteString postId, string content, Boolean isReply, UInt160 replyNFTScriptHash, ByteString replyNftTokenId);


        [DisplayName("AccountInitialized")]
        public static event OnAccountCreatedDelegate OnAccountCreated = default!;

        [DisplayName("Posted")]
        public static event OnPostedDelegate OnPosted = default!;

        public static void SetupAccountImplementation(ByteString nefFile, string manifest)
        {
            var key = new byte[] { Prefix_ContractOwner };
            var contractOwner = (UInt160)Storage.Get(Storage.CurrentContext, key);

            if (!contractOwner.Equals(Tx.Sender))
            {
                throw new Exception("Only the contract owner can update the contract");
            }
            Storage.Put(Storage.CurrentContext, "NefFile", StdLib.Serialize(nefFile));
            Storage.Put(Storage.CurrentContext, "Manifest", StdLib.Serialize(manifest));
        }

        public static Boolean CheckAccount(UInt160 scriptHash)
        {
            StorageMap accounts = new(Storage.CurrentContext, Prefix_AccountsStorage);
            Boolean? exists = (Boolean?)StdLib.Deserialize(accounts[scriptHash]);
            return exists ?? false;
        }


        public static void CreateAccount(UInt160 nftScriptHash, ByteString tokenId)
        {
            UInt160 nftOwner = (UInt160)Contract.Call(nftScriptHash, "ownerOf", CallFlags.All, tokenId);
            if (!Runtime.CheckWitness(nftOwner))
            {
                throw new Exception("Only the nft owner can create an account");
            }
            BigInteger salt = Runtime.GetRandom();


            BigInteger kind = salt % 101;
            salt = (BigInteger)salt / 100;
            BigInteger funny = salt % 101;
            salt = (BigInteger)salt / 100;
            BigInteger sad = salt % 101;
            salt = (BigInteger)salt / 100;
            BigInteger angry = salt % 101;
            salt = (BigInteger)salt / 100;

            ByteString nefFile = (ByteString)StdLib.Deserialize(Storage.Get(Storage.CurrentContext, "NefFile"));
            string manifest = (string)StdLib.Deserialize(Storage.Get(Storage.CurrentContext, "Manifest"));

            InitializeParams initParams = new InitializeParams();
            initParams.NFTContract = nftScriptHash;
            initParams.TokenId = tokenId;
            initParams.Kind = kind;
            initParams.Funny = funny;
            initParams.Sad = sad;
            initParams.Angry = angry;

            var state = ContractManagement.Deploy(nefFile, manifest, StdLib.Serialize(initParams));
            StorageMap accounts = new(Storage.CurrentContext, Prefix_AccountsStorage);

            accounts[state.Hash] = StdLib.Serialize(true);
            OnAccountCreated(nftScriptHash, Tx.Sender, tokenId, salt);
        }



        [DisplayName("_deploy")]
        public static void Deploy(object data, bool update)
        {
            if (update) return;

            var key = new byte[] { Prefix_ContractOwner };
            Storage.Put(Storage.CurrentContext, key, Tx.Sender);
        }

        public static void Update(ByteString nefFile, string manifest)
        {
            var key = new byte[] { Prefix_ContractOwner };
            var contractOwner = (UInt160)Storage.Get(Storage.CurrentContext, key);

            if (!contractOwner.Equals(Tx.Sender))
            {
                throw new Exception("Only the contract owner can update the contract");
            }

            ContractManagement.Update(nefFile, manifest, null);
        }
    }
}
