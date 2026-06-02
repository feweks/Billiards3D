using Raylib_cs;

namespace Game.Client.Data;

class ChatData
{
    private const float FIRST_BACKSPACE_TIME = 0.3f;
    private const float BACKSPACE_TIME = 0.1f;

    public string Input { get; set; } = string.Empty;
    public bool IsFocused { get; set; } = false;

    private float backspaceTimer = 0f;
    private bool firstBackspace = true;

    public ChatData()
    {

    }

    public void Update(float dt)
    {
        if (Raylib.IsKeyReleased(KeyboardKey.T))
        {
            IsFocused = true;
        }

        if (IsFocused)
        {
            KeyboardKey key = (KeyboardKey)Raylib.GetKeyPressed();
            if (key != KeyboardKey.Null)
            {
                if (key != KeyboardKey.Backspace)
                    Input += Utils.GetKeyCode(key, Raylib.IsKeyDown(KeyboardKey.LeftShift));
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Backspace))
            {
                Input = Input.Length == 0 ? Input : Input.Substring(0, Input.Length - 1);
            }

            //if ()
        }
    }
}
