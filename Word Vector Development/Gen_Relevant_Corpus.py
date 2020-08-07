import torch
import torch.optim as optim
import torch.nn as nn

import torchtext
from torchtext import data
import spacy

import argparse
import os
import pandas as pd
import scipy.spatial as spatial
from scipy.spatial import cKDTree

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

class vocab():
    def __init__(self, words, gv):
        self.all_words={word: gv.vectors[gv.stoi[word]] for word in words}
        self.not_checked=self.all_words.copy()
        self.gv=gv

    def expand(self, breadth, depth):
        tree=cKDTree(self.gv.vectors)
        for i in range(depth):
            query=torch.stack(tuple(self.not_checked.values()), dim=0)
            dd, ii= tree.query(query, k=breadth)
            new_not_checked={self.gv.itos[i]: self.gv.vectors[i] for i in np.unique(ii.flatten())}
            self.all_words={**new_not_checked, **self.all_words}
            self.not_checked=new_not_checked


def main(args):

    chat=["feminine", "feminist"]
    gv=torchtext.vocab.GloVe(name='6B', dim=100)

    other_words = ["feminine"]
    w = {word: gv.vectors[gv.stoi[word]].numpy() for word in other_words}
    new_corp = pd.DataFrame.from_dict(w, orient='index')
    new_corp.to_csv('Data/new_corpus.csv', header=None, index=True, sep=' ', mode='a')
    with open("Data/New_Corpus_Words.txt", 'a') as f:
        for t in other_words:
            f.write(t + '\n')


    # with open("Data/Tags.txt", "r") as f:
    #     tag_list=f.read().lower().split()
    #
    # new_corpus=vocab(tag_list, gv)
    # new_corpus.expand(7,3)
    # new_corp={i : new_corpus.all_words[i].numpy() for i in new_corpus.all_words}
    # new_corp= pd.DataFrame.from_dict(new_corp, orient='index')
    # new_corp.to_csv('Data/new_corpus2.csv', header=None,index=True, sep=' ', mode='w')
    # with open("Data/New_Tags2.txt", 'w') as f:
    #     for t in new_corpus.all_words.keys():
    #         f.write(t+'\n')

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
