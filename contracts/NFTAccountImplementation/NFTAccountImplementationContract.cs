﻿using System;
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
        None,
        Kind,
        Funny,
        Sad,
        Angry
    }
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

    public class Post
    {
        public string content;
        public UInt160 prompter;
        public bool isReply;
        public UInt160 replyNFTScriptHash;
        public ByteString replyNftTokenId;
        public BigInteger kind;
        public BigInteger funny;
        public BigInteger sad;
        public BigInteger angry;
        public Map<UInt160,Reaction?> reactions;
    }

    [DisplayName("Gabriel.NFTAccountImplementationContract")]
    [ManifestExtra("Author", "Gabriel Antony Xaviour")]
    [ManifestExtra("Email", "gabrielantony56@gmail.com")]
    [ManifestExtra("Description", "Describe your contract...")]
    public class NFTAccountImplementationContract : SmartContract
    {
        const byte Prefix_Personality = 0x01;
        const byte Prefix_Posts = 0x02;
        const byte Prefix_Followers = 0x03;
        const byte Prefix_Following = 0x04;



        public delegate void OnAccountInitializedDelegate(UInt160 nftScriptHash, ByteString tokenId, BigInteger kind, BigInteger funny, BigInteger sad, BigInteger angry);
        public delegate void OnPostedDelegate(UInt160 postId, string content, bool isReply, UInt160 replyNFTScriptHash, ByteString replyNftTokenId);
        public delegate void OnFollowedDelegate(UInt160 nftScriptHash);
        public delegate void OnUnfollowedDelegate(UInt160 nftScriptHash);
        public delegate void OnReactedDelegate(UInt160 postId);


        [DisplayName("AccountInitialized")]
        public static event OnAccountInitializedDelegate OnAccountInitialized = default!;

        [DisplayName("Posted")]
        public static event OnPostedDelegate OnPosted = default!;

        [DisplayName("Followed")]
        public static event OnFollowedDelegate OnFollowed = default!;

        [DisplayName("UnFollowed")]
        public static event OnUnfollowedDelegate OnUnfollowed = default!;

        [DisplayName("Reacted")]
        public static event OnReactedDelegate OnReacted = default!;

        [DisplayName("_deploy")]
        public static void Deploy(object data, bool update)
        {
            if (update) return;

            if(data==null)
            {
                return;
            }

            StorageMap personality = new(Storage.CurrentContext, Prefix_Personality);

            InitializeParams InitializeParams = (InitializeParams)StdLib.Deserialize((ByteString)data);

            Storage.Put(Storage.CurrentContext, "NFTContract", InitializeParams.NFTContract);
            Storage.Put(Storage.CurrentContext, "NFTTokenId", InitializeParams.TokenId);

            personality.Put("Kind", InitializeParams.Kind);
            personality.Put("Funny", InitializeParams.Funny);
            personality.Put("Sad", InitializeParams.Sad);
            personality.Put("Angry", InitializeParams.Angry);
            Storage.Put(Storage.CurrentContext, "RegistryAddress",InitializeParams.RegistryAddress);

            OnAccountInitialized(InitializeParams.NFTContract, InitializeParams.TokenId, InitializeParams.Kind, InitializeParams.Funny, InitializeParams.Sad, InitializeParams.Angry);
        }

       

        public static void Post(string content, bool isReply, UInt160 replyNFTScriptHash, ByteString replyNftTokenId)
        {
            
            Transaction Tx = (Transaction)Runtime.ScriptContainer;

             

            if (Tx.Sender!=GetOwner())
            {
                throw new Exception("Unauthorized");
            }
            StorageMap posts = new(Storage.CurrentContext, Prefix_Posts);
            UInt160 postId = (UInt160)CryptoLib.Ripemd160(CryptoLib.Sha256(content));

            Post post = new Post();
            post.content = content;
            post.prompter = Tx.Sender;
            post.isReply = isReply;
            post.replyNFTScriptHash = replyNFTScriptHash;
            post.replyNftTokenId = replyNftTokenId;
            post.kind = 0;
            post.funny = 0;
            post.sad = 0;
            post.angry = 0;
            post.reactions = new Map<UInt160, Reaction?>();

            posts.Put(postId,StdLib.Serialize(post));

            OnPosted(postId, content, isReply, replyNFTScriptHash, replyNftTokenId);
        }

        public static void React(UInt160 postId, Reaction reaction)
        {
            UInt160 registryAddress = (UInt160)Storage.Get(Storage.CurrentContext, "RegistryAddress");
            Transaction Tx = (Transaction)Runtime.ScriptContainer;
            
             
            if ((bool)Contract.Call(registryAddress, "checkAccount", CallFlags.All, Tx.Sender) != true)
            {
                throw new Exception("Unauthorized");
            }
            StorageMap posts = new(Storage.CurrentContext, Prefix_Posts);
            Post post=(Post)StdLib.Deserialize(posts.Get(postId));
            if (post.reactions[Tx.Sender] == null|| post.reactions[Tx.Sender] == Reaction.None)
            {
                post.reactions[Tx.Sender] = reaction;
                BigInteger? popularity = (BigInteger)StdLib.Deserialize(Storage.Get(Storage.CurrentContext, "Popularity"));
                if (popularity == null)
                {
                    popularity = 1;
                }
                else
                {
                    popularity += 1;
                }
                Storage.Put(Storage.CurrentContext, "Popularity", StdLib.Serialize(popularity));
            }
            else
            {
                Reaction oldReaction = (Reaction)post.reactions[Tx.Sender];
                if (oldReaction == Reaction.Kind)
                {
                    post.kind -= 1;
                }
                else if (oldReaction == Reaction.Funny)
                {
                    post.funny -= 1;
                }
                else if (oldReaction == Reaction.Sad)
                {
                    post.sad -= 1;
                }
                else if (oldReaction == Reaction.Angry)
                {
                    post.angry -= 1;
                }
                post.reactions[Tx.Sender] = reaction;

            }

            if (reaction == Reaction.Kind)
            {
                post.kind += 1;
            }
            else if (reaction == Reaction.Funny)
            {
                post.funny += 1;
            }
            else if (reaction == Reaction.Sad)
            {
                post.sad += 1;
            }
            else if (reaction == Reaction.Angry)
            {
                post.angry += 1;
            }

            OnReacted(postId);
        }



        public static void Follow(UInt160 scriptHash)
        {
            Transaction Tx = (Transaction)Runtime.ScriptContainer;
            
             

            if (Tx.Sender!=GetOwner())
            {
                throw new Exception("Unauthorized");
            }

            BigInteger? following = (BigInteger)StdLib.Deserialize(Storage.Get(Storage.CurrentContext, "Following"));
            StorageMap followingMap = new(Storage.CurrentContext, Prefix_Following);

            bool?  isFollowing=(bool)StdLib.Deserialize(followingMap.Get(scriptHash));

            if (isFollowing == null || isFollowing == false)
            {
                if (following == null)
                {
                    following = 1;
                }
                else
                {
                    following += 1;
                }
            }

            followingMap[scriptHash] = StdLib.Serialize(true);

            Contract.Call(scriptHash, "ReceiveFollow", CallFlags.All);
            Storage.Put(Storage.CurrentContext, "Following", StdLib.Serialize(following));
            OnFollowed(scriptHash);
        }

        public static void UnFollow(UInt160 scriptHash)
        {
            Transaction Tx = (Transaction)Runtime.ScriptContainer;
            
             
            
            if (Tx.Sender!=GetOwner())
            {
                throw new Exception("Unauthorized");
            }
            BigInteger? populatrity = (BigInteger?)StdLib.Deserialize(Storage.Get(Storage.CurrentContext, "Popularity"));

            StorageMap followingMap = new(Storage.CurrentContext, Prefix_Following);
            BigInteger? following = (BigInteger?)StdLib.Deserialize(Storage.Get(Storage.CurrentContext, "Following"));
            BigInteger? popularity =(BigInteger?)StdLib.Deserialize(Storage.Get(Storage.CurrentContext, "Popularity"));

            bool?  isFollowing=(bool)StdLib.Deserialize(followingMap.Get(scriptHash));

              
            if (isFollowing != null && isFollowing != false)
            {
                following -= 1;
                popularity = -1;
                followingMap[scriptHash] = StdLib.Serialize(false);
            }
            Contract.Call(scriptHash, "ReceiveUnFollow", CallFlags.All);

            Storage.Put(Storage.CurrentContext, "Popularity", StdLib.Serialize(popularity));
            Storage.Put(Storage.CurrentContext, "Following", StdLib.Serialize(following));
            OnUnfollowed(scriptHash);
        }

        public static void ReceiveFollow()
        {
            UInt160 registryAddress = (UInt160)Storage.Get(Storage.CurrentContext, "RegistryAddress");

            bool? isAccount= (bool?)Contract.Call(registryAddress, "checkAccount", CallFlags.All, Runtime.CallingScriptHash);

            if (Runtime.CallingScriptHash==GetOwner() ||  isAccount == false)
            {
                throw new Exception("Unauthorized");
            }

            BigInteger? popularity = (BigInteger?)StdLib.Deserialize(Storage.Get(Storage.CurrentContext, "Popularity"));
            if (popularity == null)
            {
                popularity = 1;
            }
            else
            {
                popularity += 1;
            }

            BigInteger? followers = (BigInteger?)StdLib.Deserialize(Storage.Get(Storage.CurrentContext, "Followers"));
            if (followers == null)
            {
                followers = 1;
            }
            else
            {
                followers += 1;
            }

            StorageMap followersMap = new(Storage.CurrentContext, Prefix_Followers);
            followersMap[Runtime.CallingScriptHash] = StdLib.Serialize(true);

            Storage.Put(Storage.CurrentContext, "Popularity", StdLib.Serialize(popularity));
            Storage.Put(Storage.CurrentContext, "Followers", StdLib.Serialize(followers));

        }

        public static void ReceiveUnFollow()
        {
            UInt160 registryAddress = (UInt160)Storage.Get(Storage.CurrentContext, "RegistryAddress");
            bool? isAccount= (bool?)Contract.Call(registryAddress, "checkAccount", CallFlags.All, Runtime.CallingScriptHash);

            if (Runtime.CallingScriptHash==GetOwner() || isAccount != true)
            {
                throw new Exception("Unauthorized");
            }

            BigInteger? popularity = (BigInteger?)StdLib.Deserialize(Storage.Get(Storage.CurrentContext, "Popularity"));
            if (popularity != null && popularity != 0)
            {
                popularity -= 1;
            }
            BigInteger? followers = (BigInteger?)StdLib.Deserialize(Storage.Get(Storage.CurrentContext, "Followers"));
            if (followers != null && followers != 0)
            {
                followers -= 1;
            }

            StorageMap followersMap = new(Storage.CurrentContext, Prefix_Followers);
            followersMap[Runtime.CallingScriptHash] = StdLib.Serialize(false);

            Storage.Put(Storage.CurrentContext, "Popularity", StdLib.Serialize(popularity));
            Storage.Put(Storage.CurrentContext, "Followers", StdLib.Serialize(followers));
        }

        public static UInt160 GetOwner()
        {
            UInt160 nftContract = (UInt160)Storage.Get(Storage.CurrentContext, "NFTContract");
            ByteString tokenId = Storage.Get(Storage.CurrentContext, "NFTTokenId");
            UInt160 owner=(UInt160)Contract.Call(nftContract, "ownerOf", CallFlags.All, tokenId);
            return owner;
        }

        public static void DeleteAccount()
        {

            if (Runtime.CallingScriptHash!=GetOwner())
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
