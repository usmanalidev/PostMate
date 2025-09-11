# WhatsApp Integration Guide

## Overview
The Postmate API now includes full WhatsApp integration for creating and managing LinkedIn posts through WhatsApp messages. Users can create posts, receive AI-generated drafts, and approve/reject them directly through WhatsApp.

## 🔄 Workflow

### 1. Post Creation Flow
```
User sends topic → AI generates draft → User receives confirmation → User approves/rejects
```

### 2. Message Flow
1. **User sends a topic** (any text message)
2. **System creates post** with "Draft" status
3. **AI generates draft** using OpenAI
4. **Post status changes** to "Pending"
5. **User receives confirmation** with draft and options
6. **User responds** with "1" (approve) or "0" (reject)
7. **System updates status** accordingly

## 📱 WhatsApp Message Types

### Confirmation Message
```
📝 *New Post Created!*

*Topic:* [User's Topic]

*Draft:*
[AI Generated Draft]

Reply with:
• *1* to approve and schedule
• *0* to reject
```

### Status Updates
- **Approved**: "✅ *Post Approved!* Your post has been approved and will be published shortly."
- **Rejected**: "❌ *Post Rejected* Your post has been rejected. You can create a new one anytime."
- **Posted**: "🚀 *Post Published!* Your post has been successfully published on LinkedIn!"

## 🔧 Configuration

### appsettings.json
```json
{
  "WhatsApp": {
    "AccessToken": "your-whatsapp-access-token",
    "PhoneNumberId": "your-phone-number-id"
  },
  "LinkedIn": {
    "AccessToken": "your-linkedin-access-token",
    "AuthorUrn": "urn:li:person:your-person-id"
  },
  "OpenAI": {
    "ApiKey": "your-openai-api-key"
  }
}
```

### Environment Variables (for production)
```bash
WhatsApp__AccessToken=your-token
WhatsApp__PhoneNumberId=your-phone-id
LinkedIn__AccessToken=your-token
LinkedIn__AuthorUrn=your-urn
OpenAI__ApiKey=your-key
```

## 🚀 API Endpoints

### WhatsApp Webhook
- **GET** `/api/webhook/whatsapp` - Webhook verification
- **POST** `/api/webhook/whatsapp` - Receive messages

### Webhook Verification
```
GET /api/webhook/whatsapp?hub.mode=subscribe&hub.verify_token=YOUR_TOKEN&hub.challenge=CHALLENGE
```

## 📊 Database Schema Updates

### New Post Status
- `Draft` - Initial creation, before AI generation
- `Pending` - AI draft generated, awaiting user confirmation
- `Approved` - User approved, scheduled for publishing
- `Posted` - Successfully published on LinkedIn
- `Rejected` - User rejected the post

### Updated Constraints
```sql
ALTER TABLE [dbo].[Posts] 
ADD CONSTRAINT [CK_Posts_Status] 
CHECK ([Status] IN ('Draft', 'Pending', 'Approved', 'Posted', 'Rejected'));
```

## 🔐 Security

### Webhook Verification
- Uses a secure verification token
- Validates incoming webhook requests
- Logs all verification attempts

### Message Processing
- Validates message structure
- Handles malformed messages gracefully
- Logs all message processing

## 📝 Message Examples

### User Input Examples
```
"Artificial Intelligence in Healthcare"
"Remote Work Best Practices"
"Digital Transformation Trends"
```

### AI Generated Draft Example
```
"Exciting developments in AI are revolutionizing healthcare! From diagnostic tools to personalized treatment plans, AI is helping medical professionals provide better patient care. The future of medicine is here, and it's powered by intelligent algorithms. #AI #Healthcare #Innovation #MedicalTech"
```

### User Confirmation Examples
```
"1" → Approve and schedule
"0" → Reject
```

## 🛠️ Testing

### Test Webhook Locally
1. Use ngrok to expose your local server
2. Set webhook URL in WhatsApp Business API
3. Send test messages to your WhatsApp number

### Test Message Flow
1. Send a topic message
2. Wait for confirmation message
3. Reply with "1" or "0"
4. Check database for status updates

## 📈 Monitoring

### Logs to Monitor
- Webhook verification attempts
- Message processing errors
- AI draft generation failures
- LinkedIn posting results
- WhatsApp message sending status

### Key Metrics
- Message processing time
- AI generation success rate
- User approval/rejection ratio
- LinkedIn posting success rate

## 🚨 Error Handling

### Common Issues
1. **Empty messages** - Logged and ignored
2. **AI generation failures** - User notified via WhatsApp
3. **LinkedIn posting failures** - Logged and retried
4. **WhatsApp sending failures** - Logged and retried

### Error Messages
- "❌ Sorry, I couldn't generate a draft for your topic"
- "❌ No pending posts found. Please create a new post first."
- "❌ Sorry, there was an error processing your request."

## 🔄 Background Processing

### Hangfire Jobs
- **Post Scheduler**: Checks every 5 minutes for approved posts
- **LinkedIn Publisher**: Posts approved content to LinkedIn
- **WhatsApp Notifier**: Sends status updates to users

### Job Flow
```
Approved Post → Scheduled Time Reached → LinkedIn API → Status Update → WhatsApp Notification
```

## 📱 WhatsApp Business API Setup

### Prerequisites
1. Facebook Developer Account
2. WhatsApp Business Account
3. Phone number verification
4. Webhook URL configuration

### Webhook Configuration
```
URL: https://your-domain.com/api/webhook/whatsapp
Verify Token: 4oHNU2EJAbNnM89bdM3k80QPyDBspmfsDWBdgS3U0fE=
```

## 🔧 Development

### Local Development
1. Use ngrok for webhook testing
2. Set up test WhatsApp Business API
3. Use test LinkedIn API credentials
4. Mock OpenAI responses for testing

### Production Deployment
1. Set up production WhatsApp Business API
2. Configure production LinkedIn API
3. Set up proper logging and monitoring
4. Implement rate limiting and error handling

## 📚 API Documentation

### WhatsApp Send Message API
```bash
curl --location 'https://graph.facebook.com/v22.0/{phone-number-id}/messages' \
--header 'Authorization: Bearer {access-token}' \
--header 'Content-Type: application/json' \
--data '{
  "messaging_product": "whatsapp",
  "to": "{recipient-phone-number}",
  "type": "text",
  "text": {
    "body": "{message-content}"
  }
}'
```

### LinkedIn Post API
```bash
curl --location 'https://api.linkedin.com/v2/ugcPosts' \
--header 'Authorization: Bearer {access-token}' \
--header 'X-Restli-Protocol-Version: 2.0.0' \
--header 'Content-Type: application/json' \
--data '{
  "author": "{author-urn}",
  "lifecycleState": "PUBLISHED",
  "specificContent": {
    "com.linkedin.ugc.ShareContent": {
      "shareCommentary": {
        "text": "{post-content}"
      },
      "shareMediaCategory": "NONE"
    }
  },
  "visibility": {
    "com.linkedin.ugc.MemberNetworkVisibility": "PUBLIC"
  }
}'
```

## 🎯 Best Practices

### Message Design
- Keep messages concise and clear
- Use emojis for better user experience
- Provide clear action instructions
- Handle edge cases gracefully

### Error Handling
- Always log errors with context
- Provide user-friendly error messages
- Implement retry mechanisms
- Monitor and alert on failures

### Performance
- Process messages asynchronously
- Implement proper caching
- Monitor API rate limits
- Optimize database queries

## 🔍 Troubleshooting

### Common Issues
1. **Webhook not receiving messages** - Check URL and verification token
2. **AI generation failing** - Verify OpenAI API key and credits
3. **LinkedIn posting failing** - Check access token and permissions
4. **WhatsApp messages not sending** - Verify access token and phone number ID

### Debug Steps
1. Check application logs
2. Verify API credentials
3. Test individual API calls
4. Check database for status updates
5. Monitor webhook delivery status

## 📞 Support

For issues with WhatsApp integration:
1. Check the application logs
2. Verify all API credentials
3. Test webhook connectivity
4. Review error messages
5. Check API rate limits and quotas
