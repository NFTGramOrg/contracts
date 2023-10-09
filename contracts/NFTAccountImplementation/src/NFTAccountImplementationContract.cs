using System;
using System.ComponentModel;
using System.Numerics;

using Neo;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;

namespace NFTAccountImplementation
{

    public class InitializeParams
    {
        public UInt160 NFTContract =UInt160.Zero;
        public ByteString TokenId = ByteString.Empty;
        public BigInteger Kind;
        public BigInteger Funny;
        public BigInteger Sad;
        public BigInteger Angry;
    }

    [DisplayName("Gabriel.NFTAccountImplementationContract")]
    [ManifestExtra("Author", "Gabriel Antony Xaviour")]
    [ManifestExtra("Email", "gabrielantony56@gmail.com")]
    [ManifestExtra("Description", "Describe your contract...")]
    public class NFTAccountImplementationContract : SmartContract
    {
        const byte Prefix_Personality=0x01;
        const byte Prefix_Posts=0x02;
        
        const byte Prefix_ContractOwner = 0xFF;

        private static Transaction Tx => (Transaction) Runtime.ScriptContainer;

        public delegate void OnAccountInitializedDelegate(UInt160 nftScriptHash, ByteString tokenId, BigInteger kind, BigInteger funny, BigInteger sad, BigInteger angry);
        public delegate void OnPostedDelegate(ByteString postId,string content, Boolean isReply, UInt160 replyNFTScriptHash, ByteString replyNftTokenId);

        [DisplayName("AccountInitialized")]
        public static event OnAccountInitializedDelegate OnAccountInitialized=default!;

        [DisplayName("Posted")]
        public static event OnPostedDelegate OnPosted=default!;
     
        [DisplayName("_deploy")]
        public static void Deploy(object data, bool update)
        {
            if (update) return;

            StorageMap personality=new(Storage.CurrentContext,Prefix_Personality);

            InitializeParams initParams = (InitializeParams) StdLib.Deserialize(data);

            Storgae.Put(Storage.CurrentContext,"NFTContract",initParams.NFTContract);
            Storage.Put(Storage.CurrentContext,"NFTTokenId",initParams.TokenId);

            personality["Kind"]=initParams.Kind;
            personality["Funny"]=initParams.Funny;
            personality["Sad"]=initParams.Sad;
            personality["Angry"]=initParams.Angry;

            OnAccountInitialized(initParams.NFTContract,initParams.TokenId,initParams.Kind,initParams.Funny,initParams.Sad,initParams.Angry);

        }

        public static void Post(string content, Boolean isReply, UInt160 replyNFTScriptHash, ByteString replyNftTokenId)
        {
            if(!Runtime.CheckWitness(GetOwner()))
            {
                throw new Exception("Unauthorized");
            }
            StorageMap posts = new(Storage.CurrentContext, Prefix_Posts);
            ByteString postId;// Use Keccack256
            // Create post
            // Send call to NFTGram contract to track metrics.
            OnPosted(postId,content,isReply,replyNFTScriptHash,replyNftTokenId);
        }

        public static UInt160 GetOwner(){
            UInt160 nftContract = (UInt160) Storage.Get(Storage.CurrentContext,"NFTContract");
            ByteString tokenId = (ByteString) Storage.Get(Storage.CurrentContext,"NFTTokenId");
            return Contract.Call(nftContract,"ownerOf",CallFlags.All,tokenId);
        }

        public static void DeleteAccount()
        {
            
            if(!Runtime.CheckWitness(GetOwner()))
            {
                throw new Exception("Unauthorized");
            }
            ContractManagement.Destroy();
        }
        
        public static void Update(ByteString nefFile, string manifest)
        {
            throw new Exception("Disabled");
        }       
    }
}
