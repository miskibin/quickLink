---
title: AI Assistant (Grok)
nav_order: 4
---

# AI Assistant (Grok)

<img width="694" height="333" alt="image" src="https://github.com/user-attachments/assets/a095f0df-6087-4cd6-9e89-f1f13233e562" />


## Setup

1. Get API key from [xAI Console](https://console.x.ai/)
2. Open QuickLink → Search "Settings"
3. Enter API key and save

## Usage

### Start a New Chat

1. Open QuickLink (<kbd>Ctrl+Shift+A</kbd>)
2. Type your question in the main search box
3. Select **"Start conversation"** and press <kbd>Enter</kbd>

This opens the chat panel and sends your initial prompt.

### Continue Your Last Chat

1. Open QuickLink (<kbd>Ctrl+Shift+A</kbd>)
2. Search for **"Open last conversation"** and press <kbd>Enter</kbd>

This restores the last chat from the current app session.

## Features

- **Streaming Responses** - See answers as they're generated
- **Conversation History** - Continue multi-turn conversations within the current app session
- **Direct Answers** - Configured to answer without unnecessary filler
- **Short & Focused** - Responses limited to 500 tokens for quick answers
- **Quick Restore** - The last conversation can be restored after you close/hide the chat panel
- **Fast Model** - Uses Grok 4.1 fast for quick responses

## Restore Conversation

Your last conversation is saved in memory while the app is running. To continue it:

1. Open QuickLink (press <kbd>Ctrl+Shift+A</kbd>)
2. Search for "Open last conversation"
3. Press Enter

## Examples

```
How do I parse JSON in Python?
What's async/await syntax in JavaScript?
When was Python 3.11 released?
```

## Tips

- **Be specific** - "How do I handle errors in async functions?" works better than "errors"
- **Context helps** - Mention the language/framework: "How to loop in Rust?"
- **Follow-up questions** - You can ask clarifying questions in the same conversation
- **Copy answers** - Long answers will scroll; use your terminal if you need to copy large responses

## Troubleshooting

**"API Key Invalid"**
- Verify your key is correct at [xAI Console](https://console.x.ai/)
- Check that you're not rate-limited
- Try regenerating your key

**No response**
- Check your internet connection
- Verify the API key is saved correctly
- Check for any xAI service status issues

**Slow responses**
- xAI services may be under load
- Try simpler questions first
- Wait a moment and try again

## Privacy

- Your questions are sent to xAI servers
- Conversations are processed through xAI's infrastructure
- Check xAI's privacy policy for details on data handling
- Avoid sending sensitive information (passwords, tokens, etc.)

## Cost

Using the Grok API requires:
- A free or paid xAI account
- xAI has different pricing tiers
- Check [xAI pricing](https://x.ai/) for current costs
- Free tier has rate limits

## Model Information

- **Model:** `grok-4-1-fast-non-reasoning`
- **Provider:** xAI
- **Response Format:** Streaming
- **Max Tokens:** 500 (configurable)
- **Temperature:** Optimized for accuracy
