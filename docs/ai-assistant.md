# AI Assistant (Grok)

## Setup

1. Get API key from [xAI Console](https://console.x.ai/)
2. Open QuickLink → Search "Settings"
3. Enter API key and save

## Usage

1. Open QuickLink (<kbd>Ctrl+Shift+A</kbd>)
2. Search for **"Open last conversation"** and press <kbd>Enter</kbd>
3. Type your question in the chat panel and send

If you have no saved conversation, this opens a fresh chat.

## Features

- **Streaming Responses** - See answers as they're generated
- **Conversation History** - Continue multi-turn conversations within a session
- **Direct Answers** - Configured to answer without unnecessary filler
- **Short & Focused** - Responses limited to 500 tokens for quick answers
- **Persistent Sessions** - Your last conversation is saved
- **Fast Model** - Uses Grok 4.1 fast for quick responses

## Restore Conversation

Your last conversation is automatically saved. To continue it:

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
