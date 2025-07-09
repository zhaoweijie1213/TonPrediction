// contracts/MockAggregator.sol
// SPDX-License-Identifier: MIT
pragma solidity ^0.8.20;

contract MockAggregator {
    int256 private _answer;
    uint80 private _roundId;

    constructor(int256 initPrice) {
        _answer = initPrice;
        _roundId = 1;
    }

    function latestRoundData()
        external
        view
        returns (
            uint80 roundId,
            int256 answer,
            uint256 startedAt,
            uint256 updatedAt,
            uint80 answeredInRound
        )
    {
        return (_roundId, _answer, block.timestamp, block.timestamp, _roundId);
    }

    // 方便你随时改价
    function updateAnswer(int256 newPrice) external {
        _answer = newPrice;
        _roundId += 1;
    }
}
