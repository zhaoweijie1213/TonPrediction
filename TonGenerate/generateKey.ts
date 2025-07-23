import { mnemonicToWalletKey } from '@ton/crypto';
import { TonClient, WalletContractV4, internal } from '@ton/ton';

async function main() {
    // open wallet v4 (notice the correct wallet version here)
    const mnemonic =
        'coffee public deer host another give design choose slice slab wedding finish wrist comic jaguar bring library wagon clown knock shoot solar crisp rabbit'; // your 24 secret words (replace ... with the rest of the words)
    const key = await mnemonicToWalletKey(mnemonic.split(' '));
    const wallet = WalletContractV4.create({ publicKey: key.publicKey, workchain: 0 });

    const rawAddr = wallet.address.toString({ bounceable: false, urlSafe: false });
    /* ---- ⑧ 输出结果 -------------------------------- */
    console.log("\n=== TON HD Wallet (m/44'/396'/0'/0'/0') ===");
    console.log('私钥 seed (hex) :', key.secretKey.toString('hex'));
    console.log('公钥      (hex) :', key.publicKey.toString('hex'));
    console.log('地址        :', wallet.address.toString(), '\n');
    console.log('Raw 地址        :', rawAddr, '\n');
}

main();
