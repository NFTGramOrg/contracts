using System;
using System.ComponentModel;
using System.Numerics;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;

namespace NFT
{
    [DisplayName("Gabriel.NFTContract")]
    [ManifestExtra("Author", "Gabriel Antony Xaviour")]
    [ManifestExtra("Email", "gabrielantony56@gmail.com")]
    [ManifestExtra("Description", "This is a Pokemon NFT contract")]
    [SupportedStandards("NEP-11")]
    [ContractPermission("*", "onNEP11Payment")]

    public class PokemonState : Nep11TokenState
    {
        // UInt160 Owner;

        // string Name;
        public string Description;
        public string Image;
    }

    public class NFTContract : Nep11Token<PokemonState>
    {
        const byte Prefix_ContractOwner = 0xFF;

        public override string Symbol() => "POKEMON";

        private static Transaction Tx => (Transaction)Runtime.ScriptContainer;


        public static void MintNFT(string name, string description, string image)
        {
            if(Runtime.CheckWitness(Tx.Sender) == false)
                throw new Exception("Only the contract owner can mint NFTs");
            var state = new PokemonState
            {
                Owner = Tx.Sender,
                Name = name,
                Description = description,
                Image = image
            };
            BigInteger tokenId=(BigInteger)Storage.Get(Storage.CurrentContext,"TokenId");
            if(tokenId==0)
            {
                Storage.Put(Storage.CurrentContext,"TokenId",StdLib.Serialize(1));
                tokenId=1;
                Mint((ByteString)tokenId, state);
            }else{
                BigInteger tokenId1=(BigInteger)StdLib.Deserialize(Storage.Get(Storage.CurrentContext,"TokenId"));
                Storage.Put(Storage.CurrentContext,"TokenId",StdLib.Serialize(tokenId1+1));
                Mint((ByteString)(tokenId1+1), state);
            }
        }

        [DisplayName("_deploy")]
        public static void Deploy(object data, bool update)
        {
            if (update) return;

            var key = new byte[] { Prefix_ContractOwner };
            Storage.Put(key, Tx.Sender);
        }

        public static void Update(ByteString nefFile, string manifest)
        {
            if (Runtime.CheckWitness(Tx.Sender) == false)
                throw new Exception("Only the contract owner can update the contract");

            ContractManagement.Update(nefFile, manifest, null);
        }
    }
}
