import torch
import torch.optim as optim
import torch.nn as nn

import torchtext
from torchtext import data
import spacy

import argparse
import os
import pandas as pd
from scipy import spatial

import numpy as np
torch.manual_seed(0)
np.random.seed(0)

def softmax(x):
    """Compute softmax values for each sets of scores in x."""
    return np.exp(x) / np.sum(np.exp(x), axis=0)

def pick_relevant(distances):
    d=np.array(distances)
    d=softmax(-d)
    dmax=np.max(d)
    num_tag=int(np.floor(1/dmax))
    return d.argsort()[-(min(num_tag,3)) : ][::-1]

def main(args):

    ######
    # 3.2 Processing of the data
    # the code below assumes you have processed and split the data into
    # the three files, train.tsv, validation.tsv and test.tsv
    # and those files reside in the folder named "data".
    ######
    chat=["feminine", "feminist"]
    tags=[]
    # 3.2.1

    # 4.1
    gv=torchtext.vocab.GloVe(name='6B', dim=100)
    f = open("Data/Tags.csv", "r")
    tag_list=f.read().lower().split()
    tag_list_vecs=[gv.vectors[gv.stoi[i]] for i in tag_list]
    chat_vecs=[gv.vectors[gv.stoi[i]] for i in chat]
    for i in chat_vecs:
        distances=[spatial.distance.euclidean(j, i) for j in tag_list_vecs]
        distance_idxs=pick_relevant(distances)
        tags.append([tag_list[i] for i in distance_idxs])
    print(tags)

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--batch-size', type=int, default=64)
    parser.add_argument('--lr', type=float, default=0.001)
    parser.add_argument('--epochs', type=int, default=25)
    parser.add_argument('--model', type=str, default='baseline',
                        help="Model type: baseline,rnn,cnn (Default: baseline)")
    parser.add_argument('--emb_dim', type=int, default=100)
    parser.add_argument('--rnn_hidden_dim', type=int, default=100)
    parser.add_argument('--num_filt', type=int, default=50)
#changed - to _
    args = parser.parse_args()

    main(args)
