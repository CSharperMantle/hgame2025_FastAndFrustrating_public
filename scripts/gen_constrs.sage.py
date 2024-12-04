#!/usr/bin/env sage

import gzip
import json
from base64 import b64encode

from sage.all import *
from sage.crypto.util import ascii_to_bin
from sage.matrix.special import random_unimodular_matrix

set_random_seed(int(str(ascii_to_bin("csmantle")), 2))

x = vector(ZZ, (ch for ch in input("Password:> ").encode("ascii", errors="strict")))

upper_bound = 2**8
done = False
while not done:
    try:
        print(
            "Generating A s.t. det(A)=1, upper_bound=2^{0}".format(log(upper_bound, 2))
        )
        A = random_unimodular_matrix(
            MatrixSpace(ZZ, len(x)), upper_bound=upper_bound, max_tries=1000
        )
    except ValueError:
        upper_bound <<= 1
        continue
    done = True
b = A * x

print("A:")
print("[")
for row in A:
    print(f"    {list(row)},")
print("]")
print(f"b:\n{list(b)}")
print("A^-1:")
print(A.inverse())

print("---")

print(
    b64encode(
        gzip.compress(
            json.dumps(
                {
                    "mat_a": [[int(Aij) for Aij in Ai] for Ai in A.rows()],
                    "vec_b": [int(bi) for bi in b],
                }
            ).encode("utf-8")
        )
    ).decode("ascii")
)
