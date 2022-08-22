#!/usr/bin/env perl
use strict;
use warnings;
use v5.32;
use Benchmark qw(:hireswallclock cmpthese);

# Read whole file into $file
open my $fh, '<', 'LoremIpsum.txt' or die "Cannot open file: $!\n";
my $file = do { local $/; <$fh> };
close $fh;

# Run each benchmark at least 3 seconds
cmpthese(-3, {
    'Regex' => sub { duplicatesRegex($file) },
    'Split' => sub { duplicatesSplit($file) },
});

## Program Output
#
#        Rate Regex Split
# Regex 394/s    --  -49%
# Split 769/s   95%    -

sub duplicatesRegex {
    my ( $text ) = @_;
    
    # Built Dictionary
    my %dups;
    for my $word ( $file =~ m/\w+/g ) {
        $dups{$word}++;
    }
    
    # Return an Array from it
    my @dups;
    while ( my ($key,$value) = each %dups ) {
        push @dups, $key if $value > 1;
    }
    
    return \@dups;
}

sub duplicatesSplit {
    my ( $text ) = @_;
    
    my %dups;
    for my $word (split / /, $text) {
        $dups{$word}++;
    }
    
    my @dups;
    while ( my ($key,$value) = each %dups ) {
        push @dups, $key if $value > 1;
    }
}
