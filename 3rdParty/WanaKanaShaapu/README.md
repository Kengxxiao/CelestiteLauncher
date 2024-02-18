<div align='center'>

![crabigator gif](https://piskel-imgstore-b.appspot.com/img/023a2e9e-2469-11ed-9410-9f496479153e.gif)

</div>

<div align='center'>
<h1>ワナカナ ↔️ WanaKana ↔️ わなかな</h1>
</div>

## 🐊🦀 Basic Overview

`WanaKanaShaapu` is a utility library for converting Japanese characters to the Latin ones and detecting the Japanese language in a given input. The C# version is ported from <a href="https://github.com/wanikani/wanakana" title="ft_shrek">WanaKana JS</a> v5.0.0.

## 🐊🦀 Original Documentation

[WanaKana API Documentation](https://wanakana.com/docs/global.html)

## 🐊🦀 Differences in Implementation


| Method | WanaKana JS | WanaKanaShaapu |
| --- | --- | --- |
| `IsMixed` | `isMixed(input, { passKanji: true })` | `IsMixed(input, passKanji)` 
| `StripOkurigana` | `stripOkurigana(input, { leading: false, matchKanji: '' })` | `StripOkurigana(input, leading, matchKanji)`
| `Tokenize` | 1. `tokenize(input, { detailed: true, compact: false })` </br> 2. Returns either a string array or an object | 1. `Tokenize(input, compact)` </br> 2. Returns `Tokenization` object