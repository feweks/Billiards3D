using System.Numerics;
using Game.Client.Net;
using Game.Common.Packets;
using IconFonts;
using Raylib_cs;

namespace Game.Client.Data;

class ChatData
{
    private const float FIRST_BACKSPACE_TIME = 0.3f;
    private const float BACKSPACE_TIME = 0.025f;
    private const int CHAT_TEXT_SIZE = 22;
    private const float INPUT_ALPHA_MIN = 0.6f;
    private const float INPUT_ALPHA_MAX = 1f;

    public string Input { get; set; } = string.Empty;
    public bool IsFocused { get; set; } = false;

    private Font fnt;

    private float inputAlpha = 0.5f;
    private float cursorAlpha = 1f;
    private float backspaceHeldTime = 0f;
    private bool firstBackspace = true;

    public ChatData()
    {
        fnt = Resources.GetFont("resources/gfx/fonts/pixellari.ttf");
    }

    public void Update(float dt)
    {
        if (Raylib.IsKeyReleased(KeyboardKey.T))
        {
            IsFocused = true;
        }

        if (IsFocused)
        {
            if (inputAlpha < INPUT_ALPHA_MAX)
                inputAlpha += dt;

            float t = (float)Raylib.GetTime();
            float pulseTime = 6f;
            cursorAlpha = 0.25f + 0.75f * ((MathF.Sin(t * pulseTime) * 0.5f) + 0.5f);

            KeyboardKey key = (KeyboardKey)Raylib.GetKeyPressed();
            if (key != KeyboardKey.Null)
            {
                if (key != KeyboardKey.Backspace)
                    Input += Utils.GetKeyCode(key, Raylib.IsKeyDown(KeyboardKey.LeftShift));
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Backspace))
            {
                TrimInput();
                firstBackspace = true;
            }

            if (Raylib.IsKeyDown(KeyboardKey.Backspace))
            {
                backspaceHeldTime += dt;
            }

            if (Raylib.IsKeyReleased(KeyboardKey.Backspace))
            {
                backspaceHeldTime = 0f;
            }

            float backspaceTime = firstBackspace ? FIRST_BACKSPACE_TIME : BACKSPACE_TIME;
            if (backspaceHeldTime > backspaceTime)
            {
                TrimInput();
                firstBackspace = false;
                backspaceHeldTime = 0f;
            }

            if (Raylib.IsMouseButtonPressed(MouseButton.Left) || Raylib.IsMouseButtonPressed(MouseButton.Right))
            {
                IsFocused = false;
                Input = string.Empty;
            }

            if (Raylib.IsKeyReleased(KeyboardKey.Enter) && GameClient.Lobby.IsConnected() && Input != string.Empty)
            {
                GameClient.SendLobbyPacket(new ChatMessageLobbyPacket() { Content = Input });
                IsFocused = false;
                Input = string.Empty;
            }
        }
        else
        {
            if (inputAlpha > INPUT_ALPHA_MIN)
                inputAlpha -= dt;
        }
    }

    private void TrimInput() => Input = Input.Length == 0 ? Input : Input.Substring(0, Input.Length - 1);

    public void Draw()
    {
        string bottomMsg = IsFocused ? Input : "Press [T] to type";
        Vector2 bottomMsgSize = Raylib.MeasureTextEx(fnt, bottomMsg != string.Empty ? bottomMsg : "|", CHAT_TEXT_SIZE, 1);
        float chatX = 5;
        float chatBottomY = Program.Instance!.Config.RenderHeight - bottomMsgSize.Y - chatX;

        Color textCol = Raylib.ColorAlpha(Color.White, inputAlpha);
        Color outlineCol = Raylib.ColorAlpha(Color.Black, inputAlpha);

        var bottomMsgPos = new Vector2(chatX, chatBottomY);
        Utils.DrawTextOutlined(fnt, bottomMsg, bottomMsgPos, CHAT_TEXT_SIZE, textCol, outlineCol);

        if (IsFocused)
        {
            Color cursorTextCol = Raylib.ColorAlpha(Color.White, cursorAlpha);
            Color cursorOutlineCol = Raylib.ColorAlpha(Color.Black, cursorAlpha);
            Utils.DrawTextOutlined(fnt, "|", bottomMsgPos + new Vector2(bottomMsgSize.X, 0), CHAT_TEXT_SIZE, cursorTextCol, cursorOutlineCol);
        }

        if (GameClient.Lobby.ChatHistory.Count < 1)
            return;

        float msgYOffset = 0;
        for (int i = GameClient.Lobby.ChatHistory.Count - 1; i >= 0; i--)
        {
            var msg = GameClient.Lobby.ChatHistory[i];

            string nickPrefix = "<";
            string nick = msg.Sender;
            string nickSuffix = ">";
            string msgContent = $" {msg.Content}";
            Vector2 nickPrefixSize = Raylib.MeasureTextEx(fnt, nickPrefix, CHAT_TEXT_SIZE, 1);
            Vector2 nickSize = Raylib.MeasureTextEx(fnt, nick, CHAT_TEXT_SIZE, 1);
            Vector2 nickSuffixSize = Raylib.MeasureTextEx(fnt, nickSuffix, CHAT_TEXT_SIZE, 1);
            Vector2 contentSize = Raylib.MeasureTextEx(fnt, msgContent, CHAT_TEXT_SIZE, 1);
            Vector2 msgSize = new Vector2(nickPrefixSize.X + nickSize.X + nickSuffixSize.X + contentSize.X, contentSize.Y);

            Color msgCol = Color.White;
            Color nickCol = Color.Orange;
            Color msgOutlineCol = Color.Black;

            var msgPos = new Vector2(chatX, chatBottomY - bottomMsgSize.Y - msgYOffset);
            Utils.DrawTextOutlined(fnt, nickPrefix, msgPos, CHAT_TEXT_SIZE, msgCol, msgOutlineCol);
            Utils.DrawTextOutlined(fnt, nick, msgPos + new Vector2(nickPrefixSize.X, 0), CHAT_TEXT_SIZE, nickCol, msgOutlineCol);
            Utils.DrawTextOutlined(fnt, nickSuffix, msgPos + new Vector2(nickPrefixSize.X + nickSize.X, 0), CHAT_TEXT_SIZE, msgCol, msgOutlineCol);
            Utils.DrawTextOutlined(fnt, msgContent, msgPos + new Vector2(nickPrefixSize.X + nickSize.X + nickSuffixSize.X, 0), CHAT_TEXT_SIZE, msgCol, msgOutlineCol);

            msgYOffset += msgSize.Y;
        }
    }
}
