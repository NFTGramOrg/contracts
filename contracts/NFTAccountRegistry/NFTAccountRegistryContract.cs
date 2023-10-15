using System;
using System.ComponentModel;
using System.Numerics;
using System.Runtime.CompilerServices;

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
        private const byte Prefix_AccountsStorage = 0x01;
        private const byte Prefix_Owner = 0x02;

        private const byte Prefix_Manifest=0x03;

        private const byte Prefix_ContractOwner = 0xFF;

        [InitialValue("NL2UNxotZZ3zmTYN8bSuhKDHnceYRnj6NR", Neo.SmartContract.ContractParameterType.Hash160)]
        private static readonly UInt160 InitialOwner = default; 
                
        [InitialValue("Nfu9E5vLKeqazYRLXtwygcNzEa4XR2F6Rf", Neo.SmartContract.ContractParameterType.Hash160)]
        private static readonly UInt160 NamingServiceScriptHash= default; 

        [InitialValue("0x0f43c94893f516e4e26efc493e476dcad9ac5920", Neo.SmartContract.ContractParameterType.Hash160)]
        private static readonly UInt160 NFTAccountImplemenationScriptHash = default;

        public delegate void OnAccountCreatedDelegate(UInt160 nftScriptHash, UInt160 creator, ByteString tokenId, BigInteger salt);
        public delegate void OnPostedDelegate(ByteString postId, string content, bool isReply, UInt160 replyNFTScriptHash, ByteString replyNftTokenId);

        public delegate void OnSaltTestingDelegate(BigInteger salt, BigInteger kind, BigInteger funny, BigInteger sad, BigInteger angry);

        public delegate void OnSetOwnerDelegate(UInt160 newOwner);

        [DisplayName("SetOwner")]
        public static event OnSetOwnerDelegate OnSetOwner;

        [DisplayName("AccountInitialized")]
        public static event OnAccountCreatedDelegate OnAccountCreated = default!;

        [DisplayName("Posted")]
        public static event OnPostedDelegate OnPosted = default!;

        [DisplayName("SaltTesting")]
        public static event OnSaltTestingDelegate OnSaltTesting = default!;
        
        [Safe]
        public static UInt160 GetOwner()
        {
            var currentOwner = Storage.Get(new[] { Prefix_Owner }); 

            if (currentOwner == null)
            return InitialOwner;

            return (UInt160)currentOwner;
        }

        private static bool IsOwner() =>
            Runtime.CheckWitness(GetOwner());   

        public static void SetOwner(UInt160 newOwner)
        {
            if (IsOwner() == false)
            throw new InvalidOperationException("No Authorization!");
            if (newOwner != null && newOwner.IsValid)
            {
                Storage.Put(new[] { Prefix_Owner }, newOwner);
                OnSetOwner(newOwner);
            }
        }

        public static void SetManifest(string manifest)
        {
            if (IsOwner() == false)
            throw new InvalidOperationException("No Authorization!");
            Storage.Put(new[] { Prefix_Manifest },manifest); 
        }

        public static string GetManifest()
        {
            return (string)Storage.Get(Storage.CurrentContext, new byte[] { Prefix_Manifest });
        }

        public static bool CheckAccount(){
            StorageMap accounts = new(Storage.CurrentContext, Prefix_AccountsStorage);
            bool isAccount=(bool)StdLib.Deserialize(accounts.Get(Runtime.CallingScriptHash));
            return isAccount;
        }


        public static bool CheckNftOwner(UInt160 nftScriptHash, ByteString tokenId)
        {
            UInt160 nftOwner = (UInt160)Contract.Call(nftScriptHash, "ownerOf", CallFlags.All, tokenId);
            return Runtime.CheckWitness(nftOwner);

        }

        public static void CreateAccount(UInt160 nftScriptHash, ByteString tokenId)
        {
            UInt160 nftOwner = (UInt160)Contract.Call(nftScriptHash, "ownerOf", CallFlags.All, tokenId);

            // if(Runtime.CheckWitness(nftOwner)==false)
            // {
            //     throw new Exception("Only the nft owner can create an account");
            // }
        
            // Get Randomness to initialize the account personality
            BigInteger salt = Runtime.GetRandom();


            BigInteger kind = salt%100;
            salt =(salt-kind) / 100;
            BigInteger funny = salt%100;
            salt = (salt-funny) / 100;
            BigInteger sad =  salt%100;
            salt = (salt-sad) / 100;
            BigInteger angry =  salt%100;
            salt = (salt-angry) / 100;

            OnSaltTesting(salt, kind, funny, sad, angry);

            var nftAccountImplemenationContract = ContractManagement.GetContract(NFTAccountImplemenationScriptHash);

            InitializeParams initParams = new InitializeParams
            {
                NFTContract = nftScriptHash,
                TokenId = tokenId,
                Kind = kind,
                Funny = funny,
                Sad = sad,
                Angry = angry,
                RegistryAddress = Runtime.ExecutingScriptHash
            };

            // deploy the NFTAccount contract
            string manifest=(string)Storage.Get(Storage.CurrentContext, new byte[] { Prefix_Manifest });
            var state = ContractManagement.Deploy(nftAccountImplemenationContract.Nef, manifest, StdLib.Serialize(initParams)); // HERE IS THE ERROR!!!!!
            // StorageMap accounts = new(Storage.CurrentContext, Prefix_AccountsStorage);

            // accounts[state.Hash] = StdLib.Serialize(true);

            // // Register a NNS name 
            // Map<string, object> props=(Map<string, object>)Contract.Call(nftScriptHash, "properties", CallFlags.All);
            // string name=(string)props["name"];

            // bool isAvailable=(bool)Contract.Call(NamingServiceScriptHash,"isAvailable",CallFlags.All,name);
            
            // if(isAvailable)
            // {
            //     Contract.Call(NamingServiceScriptHash,"register",CallFlags.All,"hello"+".nftgram.neo",state.Hash);
            // }

            // Emit event
            OnAccountCreated(nftScriptHash, nftOwner, tokenId, salt);
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
