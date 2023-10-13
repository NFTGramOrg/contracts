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
        public UInt160 RegistryAddress = UInt160.Zero;
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
        const byte Prefix_AccountsStorage = 0x01;
        const byte Prefix_ContractOwner = 0xFF;

        [InitialValue("0x12348376f3ad9b6ace71bde78ff237e3f3a8a1d5", Neo.SmartContract.ContractParameterType.Hash160)]
        private static readonly UInt160 NFTAccountImplemenationScriptHash = default;

        public delegate void OnAccountCreatedDelegate(UInt160 nftScriptHash, UInt160 creator, ByteString tokenId, BigInteger salt);
        public delegate void OnPostedDelegate(ByteString postId, string content, bool isReply, UInt160 replyNFTScriptHash, ByteString replyNftTokenId);

        public delegate void OnSaltTestingDelegate(BigInteger salt, BigInteger kind, BigInteger funny, BigInteger sad, BigInteger angry);


        [DisplayName("AccountInitialized")]
        public static event OnAccountCreatedDelegate OnAccountCreated = default!;

        [DisplayName("Posted")]
        public static event OnPostedDelegate OnPosted = default!;

        [DisplayName("SaltTesting")]
        public static event OnSaltTestingDelegate OnSaltTesting = default!;


        public static void CreateAccount(UInt160 nftScriptHash, BigInteger tokenId)
        {
            
            var Tx=(Transaction)Runtime.ScriptContainer;

            ByteString _tokenId=(ByteString)tokenId;
             
            UInt160 nftOwner = (UInt160)Contract.Call(nftScriptHash, "ownerOf", CallFlags.All, _tokenId);

            if(Runtime.CheckWitness(Tx.Sender)==false)
            {
                throw new Exception("Only the contract owner can create an account");
            }
            if (nftOwner!=Tx.Sender)
            {
                throw new Exception("Only the nft owner can create an account");
            }

            BigInteger salt = Runtime.GetRandom();


            BigInteger kind = salt % 101;
            salt = (BigInteger)(salt-kind) / 100;
            BigInteger funny = salt % 101;
            salt = (BigInteger)(salt-funny) / 100;
            BigInteger sad = salt % 101;
            salt = (BigInteger)(salt-sad) / 100;
            BigInteger angry = salt % 101;
            salt = (BigInteger)(salt-angry) / 100;

            OnSaltTesting(salt, kind, funny, sad, angry);

            var nftAccountImplemenationContract = ContractManagement.GetContract(NFTAccountImplemenationScriptHash);

            InitializeParams initParams = new InitializeParams();
            initParams.NFTContract = nftScriptHash;
            initParams.TokenId = _tokenId;
            initParams.Kind = kind;
            initParams.Funny = funny;
            initParams.Sad = sad;
            initParams.Angry = angry;
            initParams.RegistryAddress = Runtime.ExecutingScriptHash;

            var state = ContractManagement.Deploy(nftAccountImplemenationContract.Nef, nftAccountImplemenationContract.Manifest.ToString(), StdLib.Serialize(initParams));
            StorageMap accounts = new(Storage.CurrentContext, Prefix_AccountsStorage);

            accounts[state.Hash] = StdLib.Serialize(true);
            OnAccountCreated(nftScriptHash, Tx.Sender, _tokenId, salt);
        }



        [DisplayName("_deploy")]
        public static void Deploy(object data, bool update)
        {
            if (update) return;

            var key = new byte[] { Prefix_ContractOwner };
            var Tx=(Transaction)Runtime.ScriptContainer;
            
            Storage.Put(Storage.CurrentContext, key, Tx.Sender);
        }

        public static void Update(ByteString nefFile, string manifest)
        {
            var key = new byte[] { Prefix_ContractOwner };
            var contractOwner = (UInt160)Storage.Get(Storage.CurrentContext, key);
            var Tx=(Transaction)Runtime.ScriptContainer;

             

            if (contractOwner!=Tx.Sender)
            {
                throw new Exception("Only the contract owner can update the contract");
            }

            ContractManagement.Update(nefFile, manifest, null);
        }
    }
}
