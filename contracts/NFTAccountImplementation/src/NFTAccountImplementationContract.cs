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
    public enum Reaction : byte
    {
        Kind,
        Funny,
        Sad,
        Angry
    }
    public class InitializeParams
    {
        public UInt160 NFTContract =UInt160.Zero;
        public ByteString TokenId = ByteString.Empty;
        public BigInteger Kind;
        public BigInteger Funny;
        public BigInteger Sad;
        public BigInteger Angry;
    }

    public class Post{
        public string content;
        public UInt160 prompter;
        public Boolean isReply;
        public UInt160 replyNFTScriptHash;
        public ByteString replyNftTokenId;
        public BigInteger kind;
        public BigInteger funny;
        public BigInteger sad;
        public BigInteger angry;
        public StorageMap reactions;
    }

    [DisplayName("Gabriel.NFTAccountImplementationContract")]
    [ManifestExtra("Author", "Gabriel Antony Xaviour")]
    [ManifestExtra("Email", "gabrielantony56@gmail.com")]
    [ManifestExtra("Description", "Describe your contract...")]
    public class NFTAccountImplementationContract : SmartContract
    {
        const byte Prefix_Personality=0x01;
        const byte Prefix_Posts=0x02;
        const byte Prefix_Followers=0x03;
        const byte Prefix_Following=0x04;

        const byte Prefix_ContractOwner = 0xFF;

        private static Transaction Tx => (Transaction) Runtime.ScriptContainer;

        public delegate void OnAccountInitializedDelegate(UInt160 nftScriptHash, ByteString tokenId, BigInteger kind, BigInteger funny, BigInteger sad, BigInteger angry);
        public delegate void OnPostedDelegate(Hash160 postId,string content, Boolean isReply, UInt160 replyNFTScriptHash, ByteString replyNftTokenId);
        public delegate void OnFollowedDelegate(UInt160 nftScriptHash);
        public delegate void OnUnfollowedDelegate(UInt160 nftScriptHash);
        public delegate void OnReactedDelegate(Hash160 postId);


        [DisplayName("AccountInitialized")]
        public static event OnAccountInitializedDelegate OnAccountInitialized=default!;

        [DisplayName("Posted")]
        public static event OnPostedDelegate OnPosted=default!;

        [DisplayName("Followed")]
        public static event OnFollowedDelegate OnFollowed=default!;

        [DisplayName("UnFollowed")]
        public static event OnUnfollowedDelegate OnUnfollowed=default!;

        [DisplayName("Reacted")]
        public static event OnReactedDelegate OnReacted=default!;

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

        public static void SetRegistryAddress(UInt160 registryAddress)
        {
            var key = new byte[] { Prefix_ContractOwner };
            var contractOwner = (UInt160)Storage.Get(Storage.CurrentContext, key);

            if (!contractOwner.Equals(Runtime.CallingScriptHash))
            {
                throw new Exception("Only the contract owner can update the contract");
            }
            Storage.Put(Storage.CurrentContext,"RegistryAddress",registryAddress);
        }

        public static void Post(string content, Boolean isReply, UInt160 replyNFTScriptHash, ByteString replyNftTokenId)
        {
            if(!Runtime.CheckWitness(GetOwner()))
            {
                throw new Exception("Unauthorized");
            }
            StorageMap posts = new(Storage.CurrentContext, Prefix_Posts);
            Hash160 postId=(Hash160)CryptoLib.Ripemd160(CryptoLib.Sha256(data));
            
            Post post=new Post();
            post.content=content;
            post.prompter=Runtime.CallingScriptHash;
            post.isReply=isReply;
            post.replyNFTScriptHash=replyNFTScriptHash;
            post.replyNftTokenId=replyNftTokenId;
            post.kind=0;
            post.funny=0;
            post.sad=0;
            post.angry=0;
            post.reactions=new StorageMap(Storage.CurrentContext,postId);
            // Create post
            // Send call to NFTGram contract to track metrics.
            OnPosted(postId,content,isReply,replyNFTScriptHash,replyNftTokenId);
        }

        public static void React(ByteString postId, Reaction reaction)
        {
            UInt160 registryAddress = (UInt160) Storage.Get(Storage.CurrentContext,"RegistryAddress");
            if(Contract.call(registryAddress,'checkAccount',CallFlags.All,Runtime.CallingScriptHash)!=true)
            {
                throw new Exception("Unauthorized");
            }
            StorageMap posts = new(Storage.CurrentContext, Prefix_Posts);
            if(post.reactions[Runtime.CallingScriptHash]==null)
            {
                post.reactions[Runtime.CallingScriptHash]=reaction;
                 BigInteger populatrity=(BigInteger)Storage.Get(Storage.CurrentContext,"Popularity");
                if(popularity==null)
                {
                    popularity=1;
                }else{
                    popularity+=1;
                }
            }else{
                Reaction oldReaction=(Reaction) post.reactions[Runtime.CallingScriptHash];
                if(oldReaction==Reaction.Kind)
                {
                    post.kind-=1;
                }else if(oldReaction==Reaction.Funny)
                {
                    post.funny-=1;
                }else if(oldReaction==Reaction.Sad)
                {
                    post.sad-=1;
                }else if(oldReaction==Reaction.Angry)
                {
                    post.angry-=1;
                }
                post.reactions[Runtime.CallingScriptHash]=reaction;

            }

            if(reaction==Reaction.Kind)
            {
                post.kind+=1;
            }else if(reaction==Reaction.Funny)
            {
                post.funny+=1;
            }else if(reaction==Reaction.Sad)
            {
                post.sad+=1;
            }else if(reaction==Reaction.Angry)
            {
                post.angry+=1;
            }
           
            Storage.Put(Storage.CurrentContext,"Popularity",popularity);
            OnReacted(postId);
        }

        

        public static void Follow(UInt160 scriptHash)
        {
           if(!Runtime.CheckWitness(GetOwner()))
            {
                throw new Exception("Unauthorized");
            }
            BigInteger populatrity=(BigInteger)Storage.Get(Storage.CurrentContext,"Popularity");

            BigInteger following=(BigInteger)Storage.Get(Storage.CurrentContext,"Following");

            if(followingMap[scriptHash]==null||followingMap[scriptHash]=false)
            {
                if(following==null)
                {
                    following=1;
                }else{
                    following+=1;
                }
                if(popularity==null)
                {
                    popularity=1;
                }else{
                    popularity+=1;
                }
            }

            StorageMap followingMap = new(Storage.CurrentContext,Prefix_Following);
            followingMap[scriptHash]=true;

            Contract.Call(scriptHash,"ReceiveFollow",CallFlags.All);
            Storage.Put(Storage.CurrentContext,"Popularity",popularity);
            Storage.Put(Storage.CurrentContext,"Following",following);
            OnFollowed(scriptHash);
        }

        public static void UnFollow(UInt160 scriptHash)
        {
            if(!Runtime.CheckWitness(GetOwner()))
            {
                throw new Exception("Unauthorized");
            }
            BigInteger populatrity=(BigInteger)Storage.Get(Storage.CurrentContext,"Popularity");

            StorageMap followingMap = new(Storage.CurrentContext,Prefix_Following);
            BigInteger following=(BigInteger)Storage.Get(Storage.CurrentContext,"Following");

            if(followingMap[scriptHash]!=null&&followingMap[scriptHash]!=false)
            {
                following-=1;
                popularity=-1;
                following[scriptHash]=false;
            }

            Storage.Put(Storage.CurrentContext,"Popularity",popularity);
            Storage.Put(Storage.CurrentContext,"Following",following);
            OnUnfollowed(scriptHash);

        }

        public static void ReceiveFollow()
        {
            if(Runtime.CheckWitness(GetOwner())||Contract.Call(registryAddress,'checkAccount',CallFlags.All,Runtime.CallingScriptHash)!=true)
            {
                throw new Exception("Unauthorized");
            }

            BigInteger populatrity=(BigInteger)Storage.Get(Storage.CurrentContext,"Popularity");
            if(popularity==null)
            {
                popularity=1;
            }else{
                popularity+=1;
            }

            BigInteger followers=(BigInteger)Storage.Get(Storage.CurrentContext,"Followers");
            if(followers==null)
            {
                followers=1;
            }else{
                followers+=1;
            }

            StorageMap followersMap = new(Storage.CurrentContext,Prefix_Followers);
            followersMap[scriptHash]=true;

            Storage.Put(Storage.CurrentContext,"Popularity",popularity);
            Storage.Put(Storage.CurrentContext,"Followers",followers);

        }

         public static void ReceiveUnFollow()
        {
            if(Runtime.CheckWitness(GetOwner())||Contract.Call(registryAddress,'checkAccount',CallFlags.All,Runtime.CallingScriptHash)!=true)
            {
                throw new Exception("Unauthorized");
            }

            BigInteger populatrity=(BigInteger)Storage.Get(Storage.CurrentContext,"Popularity");
            if(popularity!=null&&popularity!=0)
            {
                popularity-=1;
            }
            BigInteger followers=(BigInteger)Storage.Get(Storage.CurrentContext,"Followers");
            if(followers!=null&&followers!=0)
            {
                followers-=1;
            }

            StorageMap followersMap = new(Storage.CurrentContext,Prefix_Followers);
            followersMap[scriptHash]=false;

            Storage.Put(Storage.CurrentContext,"Popularity",popularity);
            Storage.Put(Storage.CurrentContext,"Followers",followers);
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
