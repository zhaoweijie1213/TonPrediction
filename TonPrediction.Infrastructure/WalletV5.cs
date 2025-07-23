using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TonSdk.Contracts.Wallet;
using TonSdk.Core;
using TonSdk.Core.Block;
using TonSdk.Core.Boc;

namespace TonSdk.Contracts.wallet
{
    // Add Wallet V5R1 code constant to WalletSources
    public static class WalletSources
    {
        // ... (other wallet code constants) ...
        public const string V5R1 = "B5EE9C7201021401000281000114FF00F4A413F4BCF2C80B01020120020302014804050102F20E02" +
            "DCD020D749C120915B8F6320D70B1F2082106578746EBD21821073696E74BDB0925F03E082106578" +
            "746EBA8EB48020D72101D074D721FA4030FA44F828FA443058BD915BE0ED44D0810141D721F40583" +
            "07F40E6FA1319130E18040D721707FDB3CE03120D749810280B99130E070E2100F02012006070201" +
            "2008090019BE5F0F6A2684080A0EB90FA02C02016E0A0B0201480C0D0019ADCE76A2684020EB90EB" +
            "85FFC00019AF1DF6A2684010EB90EB858FC00017B325FB51341C75C875C2C7E00011B262FB513435" +
            "C28020011E20D70B1F82107369676EBAF2E08A7F0F01E68EF0EDA2EDFB218308D722028308D72320" +
            "8020D721D31FD31FD31FED44D0D200D31F20D31FD3FFD70A000AF90140CCF9109A28945F0ADB31E1" +
            "F2C087DF02B35007B0F2D0845125BAF2E0855036BAF2E086F823BBF2D0882292F800DE01A47FC8CA" +
            "00CB1F01CF16C9ED542092F80FDE70DB3CD81003F6EDA2EDFB02F404216E926C218E4C0221D73930" +
            "709421C700B38E2D01D72820761E436C20D749C008F2E09320D74AC002F2E09320D71D06C712C200" +
            "5230B0F2D089D74CD7393001A4E86C128407BBF2E093D74AC000F2E093ED55E2D20001C000915BE0" +
            "EBD72C08142091709601D72C081C12E25210B1E30F20D74A111213009601FA4001FA44F828FA4430" +
            "58BAF2E091ED44D0810141D718F405049D7FC8CA0040048307F453F2E08B8E14038307F45BF2E08C" +
            "22D70A00216E01B3B0F2D090E2C85003CF1612F400C9ED54007230D72C08248E2D21F2E092D200ED" +
            "44D0D2005113BAF2D08F54503091319C01810140D721D70A00F2E08EE2C8CA0058CF16C9ED5493F2" +
            "C08DE20010935BDB31E1D74CD0";
    }

    public struct WalletV5Options
    {
        public byte[] PublicKey;
        public int? Workchain;
        public uint? SubwalletId;
    }

    public struct WalletV5Storage
    {
        public bool IsSignatureAllowed;
        public uint Seqno;
        public uint SubwalletId;
        public byte[] PublicKey;
        public HashmapE<Address, bool> Extensions;
    }

    public class WalletV5 : WalletBase
    {
        private uint _subwalletId;
        public uint SubwalletId => _subwalletId;

        public WalletV5(WalletV5Options opt, uint revision = 1)
        {
            if (revision != 1)
            {
                throw new Exception("Unsupported revision. Only R1 is supported");
            }
            _code = Cell.From(WalletSources.V5R1);
            _publicKey = opt.PublicKey;
            _subwalletId = opt.SubwalletId ?? WalletTraits.SUBWALLET_ID;
            _stateInit = buildStateInit();
            _address = new Address(opt.Workchain ?? 0, _stateInit);
        }

        public WalletV5Storage ParseStorage(CellSlice slice)
        {
            // Persistent state: 1-bit signature flag, 32-bit seqno, 32-bit subwallet, 256-bit pubkey, plugins dictionary
            BlockUtils.CheckUnderflow(slice, 32 + 32 + 256 + 2, null);
            bool isSigAllowed = slice.LoadBit();
            uint seqno = (uint)slice.LoadUInt(32);
            uint subwallet = (uint)slice.LoadUInt(32);
            byte[] pubkey = slice.LoadBytes(32);
            // Load extensions dictionary (key = 256-bit address hash, value = 1-bit flag)
            var extensionsDict = slice.LoadDict(new HashmapOptions<Address, bool>
            {
                KeySize = 256,
                Deserializers = new HashmapDeserializers<Address, bool>
                {
                    Key = kb =>
                    {
                        var keySlice = kb.Parse();
                        // Assume plugin addresses on workchain 0 for simplicity
                        return new Address(0, keySlice.LoadBytes(32));
                    },
                    Value = _ => true
                }
            });
            return new WalletV5Storage
            {
                IsSignatureAllowed = isSigAllowed,
                Seqno = seqno,
                SubwalletId = subwallet,
                PublicKey = pubkey,
                Extensions = extensionsDict
            };
        }

        protected sealed override StateInit buildStateInit()
        {
            // Build initial state data with seqno=0, signature auth allowed, and no plugins
            var dataCell = new CellBuilder()
                .StoreBit(true)               // is_signature_allowed = true by default
                .StoreUInt(0, 32)            // seqno = 0 (initial)
                .StoreUInt(_subwalletId, 32)  // wallet_id (subwallet)
                .StoreBytes(_publicKey)       // 256-bit public key
                .StoreBit(false)             // empty extensions_dict
                .Build();
            return new StateInit(new StateInitOptions { Code = _code, Data = dataCell });
        }

        public struct ExtensionAction
        {
            public Address Plugin;  // plugin contract address
            public bool Add;        // true for add_ext, false for delete_ext
        }

        public ExternalInMessage CreateTransferMessage(
            WalletTransfer[] transfers, uint seqno, uint timeout = 60, ExtensionAction[]? extActions = null)
        {
            int extCount = extActions?.Length ?? 0;
            if (transfers.Length == 0 && extCount == 0)
            {
                throw new Exception("WalletV5: at least one transfer or extension action must be specified");
            }
            if (transfers.Length > 255 || extCount > 255)
            {
                throw new Exception("WalletV5: can have at most 255 transfers and 255 extension actions");
            }
            if (transfers.Length + extCount > 255)
            {
                throw new Exception("WalletV5: total actions (transfers + extensions) cannot exceed 255");
            }

            // Prepare external message body (SignedRequest structure)
            uint validUntil = (uint)DateTimeOffset.Now.ToUnixTimeSeconds() + timeout;
            var bodyBuilder = new CellBuilder()
                .StoreUInt(0, 32)                // outer opcode (0 for regular send)
                .StoreUInt(_subwalletId, 32)     // wallet_id (subwallet)
                .StoreUInt(validUntil, 32)       // valid_until timestamp
                .StoreUInt(seqno, 32);           // message seqno

            // Include outgoing transfers as OutActions (if any)
            bool hasTransfers = transfers.Length > 0;
            bodyBuilder.StoreBit(hasTransfers);
            Cell outListCell = null;
            if (hasTransfers)
            {
                // Build OutList linked list of send_msg actions:contentReference[oaicite:0]{index=0}:contentReference[oaicite:1]{index=1}
                Cell prevList = new CellBuilder().Build();  // start with empty OutList (base case)
                for (int i = 0; i < transfers.Length; i++)
                {
                    var transfer = transfers[i];
                    var itemBuilder = new CellBuilder();
                    itemBuilder.StoreRef(prevList);
                    itemBuilder.StoreUInt(0x0ec3c86d, 32);  // action_send_msg tag:contentReference[oaicite:2]{index=2}
                    itemBuilder.StoreUInt(transfer.Mode, 8);
                    itemBuilder.StoreRef(transfer.Message.Cell);
                    prevList = itemBuilder.Build();
                }
                outListCell = prevList;
                bodyBuilder.StoreRef(outListCell);
            }

            // Include plugin extension actions as ExtendedActions (if any)
            bool hasExt = extCount > 0;
            bodyBuilder.StoreBit(hasExt);
            if (hasExt)
            {
                // Build ActionList linked list of extended actions (add_ext or del_ext)
                Cell prevActions = hasTransfers ? outListCell : new CellBuilder().Build();  // base = OutList (or empty if none)
                for (int i = extCount - 1; i >= 0; i--)
                {
                    var action = extActions![i];
                    var extBuilder = new CellBuilder();
                    extBuilder.StoreRef(prevActions);
                    if (action.Add)
                    {
                        extBuilder.StoreUInt(0x02, 8);           // action_add_ext tag:contentReference[oaicite:3]{index=3}
                        extBuilder.StoreAddress(action.Plugin);
                    }
                    else
                    {
                        extBuilder.StoreUInt(0x03, 8);           // action_delete_ext tag:contentReference[oaicite:4]{index=4}
                        extBuilder.StoreAddress(action.Plugin);
                    }
                    prevActions = extBuilder.Build();
                }
                bodyBuilder.StoreRef(prevActions);
            }

            // Construct the ExternalInMessage. Include StateInit for first deploy (seqno=0).
            return new ExternalInMessage(new ExternalInMessageOptions
            {
                Info = new ExtInMsgInfo(new ExtInMsgInfoOptions { Dest = Address }),
                Body = bodyBuilder.Build(),
                StateInit = seqno == 0 ? _stateInit : null
            });
        }

        public ExternalInMessage CreateDeployMessage()
        {
            // Build an external message for initial deployment (no actions, just deploy)
            var bodyBuilder = new CellBuilder()
                .StoreUInt(0, 32)               // outer opcode = 0 (simple)
                .StoreUInt(_subwalletId, 32)    // wallet_id
                .StoreInt(-1, 32)               // valid_until = -1 (no expiration)
                .StoreUInt(0, 32)               // seqno = 0
                .StoreBit(false)               // no transfers
                .StoreBit(false);              // no extensions
            return new ExternalInMessage(new ExternalInMessageOptions
            {
                Info = new ExtInMsgInfo(new ExtInMsgInfoOptions { Dest = Address }),
                Body = bodyBuilder.Build(),
                StateInit = _stateInit          // include state init for deployment:contentReference[oaicite:5]{index=5}
            });
        }
    }
}
