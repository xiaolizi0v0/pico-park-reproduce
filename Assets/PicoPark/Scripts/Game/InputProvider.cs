using UnityEngine;

namespace PicoPark
{
    /// <summary>One control scheme: keyboard arrows or a game pad.</summary>
    public class InputProvider
    {
        public KeyCode left, right, jump, down, action;
        public int padIndex = -1;

        public InputCmd Read()
        {
            Btn b = Btn.None;
            if (padIndex >= 0)
            {
                float ax = Input.GetAxisRaw("Joy" + padIndex + "X");
                float ay = Input.GetAxisRaw("Joy" + padIndex + "Y");
                if (ax < -0.35f) b |= Btn.Left;
                if (ax > 0.35f) b |= Btn.Right;
                if (ay < -0.35f) b |= Btn.Down;
                if (ay > 0.35f) b |= Btn.Jump;
                if (Input.GetKey(KeyCode.Joystick1Button0 + padIndex * 20)) b |= Btn.Jump;
                if (Input.GetKey(KeyCode.Joystick1Button2 + padIndex * 20)) b |= Btn.Action;
            }
            else
            {
                if (Input.GetKey(left)) b |= Btn.Left;
                if (Input.GetKey(right)) b |= Btn.Right;
                if (Input.GetKey(jump)) b |= Btn.Jump;
                if (Input.GetKey(down)) b |= Btn.Down;
                if (Input.GetKey(action)) b |= Btn.Action;
            }
            InputCmd cmd = new InputCmd();
            cmd.buttons = b;
            return cmd;
        }
    }

    /// <summary>Hands out control schemes to the people sitting at this machine.</summary>
    public class LocalInputManager
    {
        /// <summary>Automation/testing hook: overrides every seat with a fixed command.</summary>
        public bool testMode;
        public Btn testButtons;
        public Btn[] testSeatButtons; // 逐座位覆盖（多人编排测试用）
        private InputProvider[] providers = new InputProvider[PPConfig.MaxPlayers];

        public LocalInputManager()
        {
            providers[0] = new InputProvider { left = KeyCode.A, right = KeyCode.D, jump = KeyCode.W, down = KeyCode.S, action = KeyCode.LeftShift };
            providers[1] = new InputProvider { left = KeyCode.LeftArrow, right = KeyCode.RightArrow, jump = KeyCode.UpArrow, down = KeyCode.DownArrow, action = KeyCode.RightShift };
            providers[2] = new InputProvider { left = KeyCode.J, right = KeyCode.L, jump = KeyCode.I, down = KeyCode.K, action = KeyCode.O };
            providers[3] = new InputProvider { left = KeyCode.F, right = KeyCode.H, jump = KeyCode.T, down = KeyCode.G, action = KeyCode.Y };
            providers[4] = new InputProvider { left = KeyCode.Keypad4, right = KeyCode.Keypad6, jump = KeyCode.Keypad8, down = KeyCode.Keypad2, action = KeyCode.Keypad0 };
            providers[5] = new InputProvider { left = KeyCode.V, right = KeyCode.N, jump = KeyCode.B, down = KeyCode.M, action = KeyCode.Space };
            providers[6] = new InputProvider { left = KeyCode.Z, right = KeyCode.C, jump = KeyCode.X, down = KeyCode.V, action = KeyCode.LeftControl };
            providers[7] = new InputProvider { left = KeyCode.Keypad1, right = KeyCode.Keypad3, jump = KeyCode.Keypad5, down = KeyCode.Keypad2, action = KeyCode.KeypadEnter };
        }

        /// <summary>P1 keeps the keyboard; extra seats borrow a connected pad when there is one.</summary>
        public void AutoAssign(int localPlayers)
        {
            string[] pads = Input.GetJoystickNames();
            int pad = 0;
            for (int i = 1; i < providers.Length && i < localPlayers; i++)
            {
                if (pads != null && pad < pads.Length && !string.IsNullOrEmpty(pads[pad]))
                {
                    providers[i].padIndex = pad;
                    pad++;
                }
                else
                {
                    providers[i].padIndex = -1;
                }
            }
            if (providers[0] != null) providers[0].padIndex = -1;
        }

        public InputCmd Read(int slot)
        {
            InputCmd cmd = new InputCmd();
            if (testMode)
            {
                if (testSeatButtons != null && slot >= 0 && slot < testSeatButtons.Length)
                    cmd.buttons = testSeatButtons[slot];
                else
                    cmd.buttons = testButtons;
                return cmd;
            }
            if (slot < 0 || slot >= providers.Length || providers[slot] == null) return InputCmd.Empty;
            return providers[slot].Read();
        }

        public InputProvider Provider(int slot)
        {
            if (slot < 0 || slot >= providers.Length) return null;
            return providers[slot];
        }
    }
}
