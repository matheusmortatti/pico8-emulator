﻿
x = 0
y = 0
r = 0
function _draw()
	cls()

    --circfill(64, 64, r)
	--line(96, r, 32, 96)
	poke4(0x4300, 100.001)
	peek4(0x4300)

    r = r + 0.1
    --x = (x + 1) % 128
    --y = (y + 1) % 128
end